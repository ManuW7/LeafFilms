import React, { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuthStore } from "../store/authStore";
import { ApiError, apiFetch } from "../lib/api";
import type { AuthResponse, User } from "../types";
import Button from "../components/ui/Button";
import Input from "../components/ui/Input";
import "./RegisterPage.css";

export default function RegisterPage() {
  const navigate = useNavigate();
  const { setAuth } = useAuthStore();
  const [form, setForm] = useState({ username: "", email: "", password: "" });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);

  const validate = () => {
    const e: Record<string, string> = {};
    if (form.username.length < 3) e.username = "Минимум 3 символа";
    if (!form.email.includes("@")) e.email = "Некорректный email";
    if (form.password.length < 8) e.password = "Минимум 8 символов";
    return e;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errs = validate();
    if (Object.keys(errs).length) {
      setErrors(errs);
      return;
    }
    setLoading(true);
    try {
      await apiFetch("POST", "/users/register", form);
      const data = await apiFetch<AuthResponse>("POST", "/users/login", {
        email: form.email,
        password: form.password,
      });
      const me = await apiFetch<User>(
        "GET",
        "/users/me",
        undefined,
        data.token,
      );
      setAuth(me, data.token);
      navigate("/");
    } catch (err) {
      setErrors({ general: err instanceof ApiError ? err.data?.error || "Ошибка регистрации" : "Ошибка регистрации" });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="register-page">
      <h1 className="register-page__title">Создать аккаунт</h1>
      <p className="register-page__subtitle">
        Присоединяйтесь к сообществу киноманов
      </p>

      <form onSubmit={handleSubmit} className="register-page__form">
        <Input
          label="Имя пользователя"
          placeholder="filmfan42"
          value={form.username}
          onChange={(e) => setForm((f) => ({ ...f, username: e.target.value }))}
          error={errors.username}
        />
        <Input
          label="Email"
          type="email"
          placeholder="you@example.com"
          value={form.email}
          onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
          error={errors.email}
        />
        <Input
          label="Пароль"
          type="password"
          placeholder="Минимум 8 символов"
          value={form.password}
          onChange={(e) => setForm((f) => ({ ...f, password: e.target.value }))}
          error={errors.password}
        />
        {errors.general && (
          <p className="register-page__error">{errors.general}</p>
        )}
        <Button
          type="submit"
          loading={loading}
          size="lg"
          className="register-page__submit"
        >
          Зарегистрироваться
        </Button>
      </form>

      <p className="register-page__footer">
        Уже есть аккаунт? <Link to="/login">Войти</Link>
      </p>
    </div>
  );
}
