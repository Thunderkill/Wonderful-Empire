import React from 'react';
import { GameStatus } from '../types';

interface GameLobbyProps {
  game: GameStatus;
  error: string | null;
  handleStartGame: () => void;
}

const GameLobby: React.FC<GameLobbyProps> = ({ game, error, handleStartGame }) => {
  const isHost = game.host.id === game.currentPlayer.id;
  const playerCount = game.otherPlayers.length + 1;
  const canStartGame = isHost && playerCount >= 2;

  return (
    <div className="game-lobby">
      <h2>Game Lobby</h2>
      {error && <p className="error-message">{error}</p>}
      <p>Players: {playerCount} / {game.host.resources?.maxPlayers || 5}</p>
      <h3>Players in Lobby:</h3>
      <ul>
        <li>{game.currentPlayer.name} {isHost ? "(Host)" : ""}</li>
        {game.otherPlayers.map(player => (
          <li key={player.id}>{player.name}</li>
        ))}
      </ul>
      {isHost ? (
        <button onClick={handleStartGame} disabled={!canStartGame}>
          Start Game
        </button>
      ) : (
        <p>Waiting for the host to start the game...</p>
      )}
      {!canStartGame && isHost && (
        <p>At least 2 players are required to start the game.</p>
      )}
    </div>
  );
};

export default GameLobby;