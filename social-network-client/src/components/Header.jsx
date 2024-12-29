import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/AuthContext';
import Button from '../components/UI/buttons/Button'

export default function Header() {
  const { logout } = useAuth();
  const navigate = useNavigate();

  return (
    <header className="bg-blue-500 text-white flex justify-between items-center">
      <div className="text-lg font-bold pl-3">
        <Button onClick={() => navigate('/')}>SD</Button>
      </div>

      <nav className="flex gap-4">
        <Link to={'/chats'} className="hover:underline">
            Чаты
        </Link>
      </nav>

      <Button onClick={logout}>Выйти</Button>
    </header>
  );
}