import { API_BASE_URL } from "../utils/apiHelper";

export const login = async (username, passwordHash) => {
  const response = await fetch(`${API_BASE_URL}/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, passwordHash }),
  });
  if (!response.ok) {
    const errorData = await response.json();
    const errorMessage = errorData.message;
    throw new Error(errorMessage);
  }
  return response.json();
};

export const register = async (username, passwordHash) => {
  const response = await fetch(`${API_BASE_URL}/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({username, passwordHash }),
  });
  if (!response.ok) {
    const errorMessage = errorData.message;
    throw new Error(errorMessage);
  }
  return '';
};

export const logout = () => {
  localStorage.removeItem('token');
};