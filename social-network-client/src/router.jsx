import { BrowserRouter, Route, Routes } from 'react-router-dom';
import PrivateRoute from './components/PrivateRoute';
import MainLayout from './layouts/MainLayout';
import Login from './pages/Login';
import Register from './pages/Register';
import Home from './pages/Home';
import Chats from './pages/Chats';
import Chat from './pages/Chat';

function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />

        <Route path="/" element={
          <PrivateRoute>
            <MainLayout showHeader={true}>
              <Home />
            </MainLayout>
          </PrivateRoute>}
        />

        <Route path="/blog/:id" element={
          <PrivateRoute>
            <MainLayout showHeader={true}>
              <Home />
            </MainLayout>
          </PrivateRoute>}
        />

        <Route path="/chats" element={
          <PrivateRoute>
            <MainLayout showHeader={true}>
              <Chats />
            </MainLayout>
          </PrivateRoute>}
        />

        <Route path="/chat/:id" element={
          <PrivateRoute>
            <MainLayout showHeader={false}>
              <Chat />
            </MainLayout>
          </PrivateRoute>}
        />
      </Routes>
    </BrowserRouter>
  );
}

export default AppRouter;