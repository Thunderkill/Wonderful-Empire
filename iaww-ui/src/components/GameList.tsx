import React, { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import api from '../api/axiosConfig';

interface GameSummary {
  id: string;
  playerCount: number;
  currentRound: number;
  currentPhase: number;
  state: number;
  maxPlayers: number;
  hasCurrentUserJoined: boolean;
}

const GameList: React.FC = () => {
  const [games, setGames] = useState<GameSummary[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [maxPlayers, setMaxPlayers] = useState<number>(2);
  const navigate = useNavigate();

  useEffect(() => {
    fetchGames();
  }, []);

  const fetchGames = async () => {
    try {
      const response = await api.get('/api/Game/list');
      setGames(response.data);
    } catch (error) {
      console.error('Failed to fetch games:', error);
      setError('Failed to fetch games. Please try again later.');
    }
  };

  const createLobby = async () => {
    try {
      const response = await api.post('/api/Game/create-lobby', { maxPlayers });
      navigate(`/game/${response.data.id}`);
    } catch (error) {
      console.error('Failed to create lobby:', error);
      setError('Failed to create lobby. Please try again.');
    }
  };

  const joinGame = async (gameId: string) => {
    try {
      await api.post(`/api/Game/${gameId}/join`);
      navigate(`/game/${gameId}`);
    } catch (error) {
      console.error('Failed to join game:', error);
      setError('Failed to join game. Please try again.');
    }
  };

  const viewGame = async (gameId: string) => {
    navigate(`/game/${gameId}`);
  };

  const getGameStateString = (state: number): string => {
    const states = ['Lobby', 'InProgress', 'Finished'];
    return states[state] || 'Unknown';
  };

  const getGamePhaseString = (phase: number): string => {
    const phases = ['Drafting', 'Planning', 'Production', 'End of Round'];
    return phases[phase] || 'Unknown';
  };

  return (
    <div className="game-list">
      <h2>Game List</h2>
      {error && <p className="error-message">{error}</p>}
      {games.length === 0 ? (
        <p>No games available. Create a new lobby to get started!</p>
      ) : (
        <ul>
          {games.map((game) => (
            <li key={game.id}>
              Game {game.id} - Players: {game.playerCount}/{game.maxPlayers} - 
              State: {getGameStateString(game.state)} - 
              {game.state === 1 && (
                <>Round: {game.currentRound} - Phase: {getGamePhaseString(game.currentPhase)}</>
              )}
              {game.state === 0 && game.playerCount < game.maxPlayers && !game.hasCurrentUserJoined && (
                <button onClick={() => joinGame(game.id)}>Join</button>
              )}
              {(game.state !== 0 || game.hasCurrentUserJoined) && (
                <button onClick={() => viewGame(game.id)}>View</button>
              )}
            </li>
          ))}
        </ul>
      )}
      <div className="create-lobby">
        <h3>Create New Lobby</h3>
        <label>
          Max Players:
          <input
            type="number"
            min="2"
            max="5"
            value={maxPlayers}
            onChange={(e) => setMaxPlayers(parseInt(e.target.value))}
          />
        </label>
        <button onClick={createLobby}>Create Lobby</button>
      </div>
    </div>
  );
};

export default GameList;