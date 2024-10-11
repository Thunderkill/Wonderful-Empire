import React from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import Login from './components/Login';
import Register from './components/Register';
import GameList from './components/GameList';
import GameBoard from './components/GameBoard';
import './App.css';

const App: React.FC = () => {
  return (
    <Router>
      <div className="app">
        <h1 className="app-title">It's a Wonderful World</h1>
        <Routes>
          <Route path="/" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route path="/games" element={<GameList />} />
          <Route path="/game/:gameId" element={<GameBoard />} />
        </Routes>
      </div>
    </Router>
  );
};

export default App;