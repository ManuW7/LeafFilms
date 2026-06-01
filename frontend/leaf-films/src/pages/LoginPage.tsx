import React, { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuthStore } from "../store/authStore";
import { apiFetch } from "../lib/api";
import type { AuthResponse, User } from "../types";
import Button from "../components/ui/Button";
import Input from "../components/ui/Input";
import "./LoginPage.css";

export default function LoginPage() {
  const navigate = useNavigate();
  const { setAuth } = useAuthStore();
  const [form, setForm] = useState({ email: "", password: "" });
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      const data = await apiFetch<AuthResponse>("POST", "/users/login", form);
      const me = await apiFetch<User>(
        "GET",
        "/users/me",
        undefined,
        data.token,
      );
      setAuth(me, data.token);
      navigate("/");
    } catch {
      setError("Неверный email или пароль");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <h1 className="login-page__title">Добро пожаловать</h1>
      <p className="login-page__subtitle">Войдите в свой аккаунт CineGram</p>

      <form onSubmit={handleSubmit} className="login-page__form">
        <Input
          label="Email"
          type="email"
          placeholder="you@example.com"
          value={form.email}
          onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
          required
        />
        <Input
          label="Пароль"
          type="password"
          placeholder="••••••••"
          value={form.password}
          onChange={(e) => setForm((f) => ({ ...f, password: e.target.value }))}
          required
        />
        {error && <p className="login-page__error">{error}</p>}
        <Button
          type="submit"
          loading={loading}
          size="lg"
          className="login-page__submit"
        >
          Войти
        </Button>
      </form>

      <p className="login-page__footer">
        Нет аккаунта? <Link to="/register">Зарегистрироваться</Link>
      </p>
    </div>
  );
}
