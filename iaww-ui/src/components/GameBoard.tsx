import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import api from '../api/axiosConfig';

interface Card {
  id: string;
  name: string;
  type: number;
  constructionCost: { [key: string]: number };
  production: { [key: string]: number };
  victoryPoints: number;
  recyclingBonus: { [key: string]: number };
  specialAbility: number;
}

interface PlayerStatus {
  id: string;
  name: string;
  resources: { [key: string]: number };
  hand: Card[];
  handCount: number;
  constructionArea: Card[];
  empire: Card[];
  isReady: boolean;
}

interface GameStatus {
  gameId: string;
  gameState: number;
  currentPhase: number;
  currentRound: number;
  host: PlayerStatus;
  currentPlayer: PlayerStatus;
  otherPlayers: PlayerStatus[];
}

const GameBoard: React.FC = () => {
  const { gameId } = useParams<{ gameId: string }>();
  const [game, setGame] = useState<GameStatus | null>(null);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    fetchGameStatus();
    const intervalId = setInterval(fetchGameStatus, 5000); // Poll every 5 seconds
    return () => clearInterval(intervalId);
  }, [gameId]);

  const fetchGameStatus = async () => {
    try {
      const response = await api.get(`/api/Game/${gameId}`);
      setGame(response.data);
    } catch (error) {
      console.error('Failed to fetch game status:', error);
      setError('Failed to fetch game status. Please try again.');
    }
  };

  const handleStartGame = async () => {
    try {
      await api.post(`/api/Game/${gameId}/start`);
      fetchGameStatus();
    } catch (error) {
      console.error('Failed to start game:', error);
      setError('Failed to start game. Please try again.');
    }
  };

  const handleDraft = async (cardId: string) => {
    try {
      const response = await api.post(`/api/Game/${gameId}/draft`, {
        playerId: game?.currentPlayer.id,
        cardId
      });
      if (response.data.success) {
        fetchGameStatus();
      } else {
        setError('Failed to draft card. Please try again.');
      }
    } catch (error) {
      console.error('Failed to draft card:', error);
      setError('Failed to draft card. Please try again.');
    }
  };

  const handleAddResource = async (cardId: string, resourceType: number) => {
    try {
      const response = await api.post(`/api/Game/${gameId}/addresource`, {
        playerId: game?.currentPlayer.id,
        cardId: cardId,
        resourceType: resourceType
      });
      
      if (response.data.success) {
        fetchGameStatus();
      } else {
        setError('Failed to add resource. Please try again.');
      }
    } catch (error) {
      console.error('Failed to add resource:', error);
      setError('Failed to add resource. Please try again.');
    }
  };

  const handleDiscard = async (cardId: string) => {
    try {
      const response = await api.post(`/api/Game/${gameId}/discard`, {
        playerId: game?.currentPlayer.id,
        cardId: cardId
      });
      
      if (response.data.success) {
        fetchGameStatus();
      } else {
        setError('Failed to discard card. Please try again.');
      }
    } catch (error) {
      console.error('Failed to discard card:', error);
      setError('Failed to discard card. Please try again.');
    }
  };

  const handleReady = async () => {
    try {
      await api.post(`/api/Game/${gameId}/ready`);
      fetchGameStatus();
    } catch (error) {
      console.error('Failed to set ready state:', error);
      setError('Failed to set ready state. Please try again.');
    }
  };

  const getCardTypeString = (type: number): string => {
    const types = ['Materials', 'Energy', 'Science', 'Gold', 'Exploration'];
    return types[type] || 'Unknown';
  };

  const renderResourceButtons = (card: Card) => {
    if (!game) return null;
    const resourceTypes = ['Materials', 'Energy', 'Science', 'Gold', 'Exploration', 'Krystallium'];
    return (
      <div className="resource-buttons">
        {Object.entries(card.constructionCost).map(([resource, cost]) => (
          <button
            key={resource}
            onClick={() => handleAddResource(card.id, resourceTypes.indexOf(resource))}
            disabled={game.currentPlayer.resources[resource] < 1}
          >
            Add {resource}
          </button>
        ))}
      </div>
    );
  };

  if (!game) {
    return <div>Loading...</div>;
  }

  if (game.gameState === 0) {
    // Lobby state
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
  }

  // In-game state
  return (
    <div className="game-board">
      <h2>Game {game.gameId}</h2>
      {error && <p className="error-message">{error}</p>}
      <div className="game-info">
        <p>Round: {game.currentRound}</p>
        <p>Phase: {game.currentPhase}</p>
        <p>State: {game.gameState}</p>
      </div>
      <div className="player-info">
        <h3>Your Information</h3>
        <p>Name: {game.currentPlayer.name}</p>
        <p>Resources:</p>
        <ul>
          {Object.entries(game.currentPlayer.resources).map(([resource, amount]) => (
            <li key={resource}>{resource}: {amount}</li>
          ))}
        </ul>
      </div>
      
      {game.gameState === 1 && game.currentPhase === 0 && (
        <div className="player-hand">
          <h3>Your Hand</h3>
          <div className="hand">
            {game.currentPlayer.hand.map((card) => (
              <div key={card.id} className="card" onClick={() => handleDraft(card.id)}>
                <h4>{card.name}</h4>
                <p>Type: {getCardTypeString(card.type)}</p>
                <p>Victory Points: {card.victoryPoints}</p>
              </div>
            ))}
          </div>
        </div>
      )}
      
      {game.gameState === 1 && game.currentPhase === 1 && (
        <div className="player-actions">
          <h3>Your Construction Area</h3>
          <div className="construction-area">
            {game.currentPlayer.constructionArea.map((card) => (
              <div key={card.id} className="card">
                <h4>{card.name}</h4>
                <p>Type: {getCardTypeString(card.type)}</p>
                <p>Victory Points: {card.victoryPoints}</p>
                <h5>Construction Cost:</h5>
                <ul>
                  {Object.entries(card.constructionCost).map(([resource, amount]) => (
                    <li key={resource}>{resource}: {amount}</li>
                  ))}
                </ul>
                {renderResourceButtons(card)}
                <button onClick={() => handleDiscard(card.id)}>Discard</button>
              </div>
            ))}
          </div>
          <button onClick={handleReady} disabled={game.currentPlayer.isReady}>
            {game.currentPlayer.isReady ? "Ready" : "Set Ready"}
          </button>
        </div>
      )}
      
      <div className="other-players">
        <h3>Other Players</h3>
        {game.otherPlayers.map((player) => (
          <div key={player.id} className="player">
            <h4>{player.name}</h4>
            <p>Hand Count: {player.handCount}</p>
            <p>Construction Area Count: {player.constructionArea.length}</p>
            <p>Empire Count: {player.empire.length}</p>
            <p>Ready: {player.isReady ? "Yes" : "No"}</p>
          </div>
        ))}
      </div>
    </div>
  );
};

export default GameBoard;
