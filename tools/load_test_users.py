#!/usr/bin/env python3
import argparse
import csv
import json
import statistics
import time
import urllib.error
import urllib.request
from concurrent.futures import ThreadPoolExecutor, as_completed
from dataclasses import dataclass
from pathlib import Path
from typing import Any
from uuid import uuid4


@dataclass
class StepResult:
    name: str
    status: int
    elapsed_ms: float
    error: str = ""


@dataclass
class UserResult:
    index: int
    username: str
    email: str
    ok: bool
    steps: list[StepResult]


def request_json(
    method: str,
    url: str,
    body: dict[str, Any] | None = None,
    token: str | None = None,
    timeout: float = 10.0,
) -> tuple[int, dict[str, Any] | list[Any] | None, float, str]:
    data = None
    headers = {"Accept": "application/json"}
    if body is not None:
        data = json.dumps(body).encode("utf-8")
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = f"Bearer {token}"

    started = time.perf_counter()
    req = urllib.request.Request(url, data=data, method=method, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=timeout) as response:
            raw = response.read()
            elapsed_ms = (time.perf_counter() - started) * 1000
            if not raw:
                return response.status, None, elapsed_ms, ""
            try:
                return response.status, json.loads(raw.decode("utf-8")), elapsed_ms, ""
            except json.JSONDecodeError:
                return response.status, None, elapsed_ms, raw[:200].decode("utf-8", errors="replace")
    except urllib.error.HTTPError as exc:
        elapsed_ms = (time.perf_counter() - started) * 1000
        raw = exc.read()
        message = raw[:300].decode("utf-8", errors="replace") if raw else exc.reason
        return exc.code, None, elapsed_ms, message
    except Exception as exc:
        elapsed_ms = (time.perf_counter() - started) * 1000
        return 0, None, elapsed_ms, str(exc)


def run_user(index: int, args: argparse.Namespace, run_id: str) -> UserResult:
    username = f"{args.prefix}_{run_id}_{index:05d}"
    email = f"{username}@load.local"
    password = args.password
    steps: list[StepResult] = []
    token = None

    register_body = {"username": username, "email": email, "password": password}
    status, _, elapsed, error = request_json(
        "POST",
        f"{args.base_url}/api/users/register",
        register_body,
        timeout=args.timeout,
    )
    steps.append(StepResult("register", status, elapsed, error))

    if status not in (200, 201, 409):
        return UserResult(index, username, email, False, steps)

    login_body = {"email": email, "password": password}
    status, payload, elapsed, error = request_json(
        "POST",
        f"{args.base_url}/api/users/login",
        login_body,
        timeout=args.timeout,
    )
    steps.append(StepResult("login", status, elapsed, error))

    if isinstance(payload, dict):
        token_value = payload.get("token")
        if isinstance(token_value, str):
            token = token_value

    if status != 200 or not token:
        return UserResult(index, username, email, False, steps)

    if args.scenario in ("mixed", "read"):
        for name, path in (
            ("movies", "/api/movies"),
            ("feed", "/api/feed?page=1&pageSize=20"),
            ("me", "/api/users/me"),
        ):
            status, _, elapsed, error = request_json(
                "GET",
                f"{args.base_url}{path}",
                token=token,
                timeout=args.timeout,
            )
            steps.append(StepResult(name, status, elapsed, error))

    return UserResult(index, username, email, True, steps)


def percentile(values: list[float], pct: float) -> float:
    if not values:
        return 0.0
    if len(values) == 1:
        return values[0]
    ordered = sorted(values)
    rank = (len(ordered) - 1) * pct
    low = int(rank)
    high = min(low + 1, len(ordered) - 1)
    weight = rank - low
    return ordered[low] * (1 - weight) + ordered[high] * weight


def print_summary(results: list[UserResult], started: float) -> None:
    total_elapsed = time.perf_counter() - started
    total = len(results)
    ok = sum(1 for result in results if result.ok)
    failed = total - ok
    print()
    print("=== Summary ===")
    print(f"Users attempted: {total}")
    print(f"Users successful: {ok}")
    print(f"Users failed: {failed}")
    print(f"Total time: {total_elapsed:.2f}s")
    if total_elapsed > 0:
        print(f"Throughput: {total / total_elapsed:.2f} users/s")

    by_step: dict[str, list[StepResult]] = {}
    for result in results:
        for step in result.steps:
            by_step.setdefault(step.name, []).append(step)

    print()
    print("=== Steps ===")
    for name, steps in sorted(by_step.items()):
        latencies = [step.elapsed_ms for step in steps]
        statuses: dict[int, int] = {}
        for step in steps:
            statuses[step.status] = statuses.get(step.status, 0) + 1
        print(
            f"{name:10} count={len(steps):6} "
            f"avg={statistics.mean(latencies):8.1f}ms "
            f"p50={percentile(latencies, 0.50):8.1f}ms "
            f"p95={percentile(latencies, 0.95):8.1f}ms "
            f"p99={percentile(latencies, 0.99):8.1f}ms "
            f"statuses={statuses}"
        )

    errors = [
        (result.index, step.name, step.status, step.error)
        for result in results
        for step in result.steps
        if step.status >= 400 or step.status == 0
    ]
    if errors:
        print()
        print("=== First errors ===")
        for index, name, status, error in errors[:20]:
            print(f"user={index} step={name} status={status} error={error}")


def save_users(path: Path, results: list[UserResult], password: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", newline="", encoding="utf-8") as file:
        writer = csv.writer(file)
        writer.writerow(["index", "username", "email", "password", "ok"])
        for result in results:
            writer.writerow([result.index, result.username, result.email, password, result.ok])


def main() -> int:
    parser = argparse.ArgumentParser(description="Load test LeafFilms with generated users.")
    parser.add_argument("--base-url", default="http://localhost", help="Gateway base URL.")
    parser.add_argument("--users", type=int, default=10_000, help="Number of virtual users.")
    parser.add_argument("--concurrency", type=int, default=100, help="Parallel workers.")
    parser.add_argument("--scenario", choices=("register", "mixed", "read"), default="mixed")
    parser.add_argument("--prefix", default="loaduser", help="Username/email prefix.")
    parser.add_argument("--password", default="Password123!", help="Password for generated users.")
    parser.add_argument("--timeout", type=float, default=15.0, help="Per-request timeout in seconds.")
    parser.add_argument("--progress-every", type=int, default=500, help="Progress print interval.")
    parser.add_argument("--save-users", default="", help="Optional CSV path for generated users.")
    args = parser.parse_args()

    if args.users <= 0:
        raise SystemExit("--users must be positive")
    if args.concurrency <= 0:
        raise SystemExit("--concurrency must be positive")

    args.base_url = args.base_url.rstrip("/")
    run_id = uuid4().hex[:8]
    started = time.perf_counter()
    results: list[UserResult] = []

    print(
        f"Starting load test: users={args.users}, concurrency={args.concurrency}, "
        f"scenario={args.scenario}, base_url={args.base_url}, run_id={run_id}"
    )

    with ThreadPoolExecutor(max_workers=args.concurrency) as executor:
        futures = [executor.submit(run_user, index, args, run_id) for index in range(args.users)]
        for completed, future in enumerate(as_completed(futures), start=1):
            result = future.result()
            results.append(result)
            if completed % args.progress_every == 0 or completed == args.users:
                ok = sum(1 for item in results if item.ok)
                print(f"progress {completed}/{args.users}, ok={ok}, failed={completed - ok}")

    results.sort(key=lambda item: item.index)
    print_summary(results, started)

    if args.save_users:
        save_users(Path(args.save_users), results, args.password)
        print(f"Saved generated users to {args.save_users}")

    return 0 if all(result.ok for result in results) else 1


if __name__ == "__main__":
    raise SystemExit(main())
