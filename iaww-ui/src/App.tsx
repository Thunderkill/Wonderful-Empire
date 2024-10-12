import React from 'react';
import { BrowserRouter as Router, Route, Routes, useNavigate } from 'react-router-dom';
import Login from './components/Login';
import Register from './components/Register';
import GameList from './components/GameList';
import GameBoard from './components/GameBoard';
import './App.css';

const AppContent: React.FC = () => {
  const navigate = useNavigate();

  const handleTitleClick = () => {
    navigate('/games');
  };

  return (
    <div className="app">
      <div onClick={handleTitleClick} style={{ cursor: 'pointer' }}>
        <h1 className="app-title">It's a Wonderful World</h1>
      </div>
      <Routes>
        <Route path="/" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/games" element={<GameList />} />
        <Route path="/game/:gameId" element={<GameBoard />} />
      </Routes>
    </div>
  );
};

const App: React.FC = () => {
  return (
    <Router>
      <AppContent />
    </Router>
  );
};

export default App;
