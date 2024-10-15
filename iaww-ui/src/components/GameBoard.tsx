import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import api from '../api/axiosConfig';
import { GameStatus, ResourceType } from '../types';
import GameLobby from './GameLobby';
import EndGameScreen from './EndGameScreen';
import PlayerInfo from './PlayerInfo';
import Empire from './Empire';
import DraftingArea from './DraftingArea';
import ConstructionArea from './ConstructionArea';
import CardDetails from './CardDetails';
import './GameBoard.css';

const GameBoard: React.FC = () => {
  const { gameId } = useParams<{ gameId: string }>();
  const [game, setGame] = useState<GameStatus | null>(null);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    fetchGameStatus();
    const intervalId = setInterval(fetchGameStatus, 500); // Poll every 0.5 seconds
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
    if (!game || game.currentPlayer.hasDraftedThisRound) return;

    try {
      const response = await api.post(`/api/Game/${gameId}/draft`, {
        playerId: game.currentPlayer.id,
        cardId
      });
      if (response.data.success) {
        setGame(prevGame => {
          if (!prevGame) return null;
          return {
            ...prevGame,
            currentPlayer: {
              ...prevGame.currentPlayer,
              hasDraftedThisRound: true
            }
          };
        });
        fetchGameStatus();
      } else {
        setError('Failed to draft card. Please try again.');
      }
    } catch (error) {
      console.error('Failed to draft card:', error);
      setError('Failed to draft card. Please try again.');
    }
  };

  const handleAddResource = async (cardId: string, resourceType: ResourceType) => {
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

  const handleMoveToConstruction = async (cardId: string) => {
    try {
      const response = await api.post(`/api/Game/${gameId}/moveToConstruction`, {
        playerId: game?.currentPlayer.id,
        cardId: cardId
      });
      
      if (response.data.success) {
        fetchGameStatus();
      } else {
        setError('Failed to move card to construction area. Please try again.');
      }
    } catch (error) {
      console.error('Failed to move card to construction area:', error);
      setError('Failed to move card to construction area. Please try again.');
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

  const renderProductionPhase = () => {
    if (!game || game.currentPhase !== 2 || game.currentProductionStep === null) return null;

    const productionOrder = [ResourceType.Materials, ResourceType.Energy, ResourceType.Science, ResourceType.Gold, ResourceType.Exploration];
    const currentStep = game.currentProductionStep;

    return (
      <div className="production-phase">
        <h3>Production Phase</h3>
        <p>Current step: {ResourceType[currentStep]}</p>
        <div className="production-steps">
          {productionOrder.map((step) => (
            <div key={step} className={`production-step ${step === currentStep ? 'active' : ''} ${step < currentStep ? 'completed' : ''}`}>
              {ResourceType[step]}
            </div>
          ))}
        </div>
        <button onClick={handleReady} disabled={game.currentPlayer.isReady}>
          {game.currentPlayer.isReady ? "Ready for Next Step" : "Set Ready"}
        </button>
      </div>
    );
  };

  if (!game) {
    return <div>Loading...</div>;
  }

  if (game.gameState === 0) {
    return <GameLobby game={game} error={error} handleStartGame={handleStartGame} />;
  }

  if (game.gameState === 2) {
    return <EndGameScreen game={game} navigate={navigate} />;
  }

  // In-game state
  return (
    <div className="game-board">
      <h2>Game {game.gameId}</h2>
      {error && <p className="error-message">{error}</p>}
      <div className="game-info">
        <p>Round: {game.currentRound}</p>
        <p>Phase: {game.currentPhase === 0 ? 'Draft' : game.currentPhase === 1 ? 'Planning' : 'Production'}</p>
        <p>State: {game.gameState === 1 ? 'In Progress' : 'Unknown'}</p>
      </div>
      
      <PlayerInfo player={game.currentPlayer} />
      <Empire player={game.currentPlayer} currentPlayerId={game.currentPlayer.id} />
      
      {game.gameState === 1 && game.currentPhase === 0 && (
        <div className="player-hand">
          <h3>Your Hand</h3>
          <div className="hand">
            {game.currentPlayer.hand.map((card) => (
              <div 
                key={card.id} 
                className={`card ${game.currentPlayer.hasDraftedThisRound ? 'disabled' : ''}`} 
                onClick={() => !game.currentPlayer.hasDraftedThisRound && handleDraft(card.id)}
              >
                <CardDetails card={card} />
              </div>
            ))}
          </div>
          {game.currentPlayer.hasDraftedThisRound && <p>You have drafted a card this round. Waiting for other players...</p>}
        </div>
      )}
      
      {game.gameState === 1 && (game.currentPhase === 1 || game.currentPhase === 2) && (
        <div className="player-actions">
          {game.currentPhase === 1 && (
            <DraftingArea 
              player={game.currentPlayer} 
              currentPlayerId={game.currentPlayer.id}
              handleMoveToConstruction={handleMoveToConstruction}
              handleDiscard={handleDiscard}
            />
          )}
          
          <ConstructionArea 
            player={game.currentPlayer}
            currentPlayerId={game.currentPlayer.id}
            handleAddResource={handleAddResource}
          />
          
          {game.currentPhase === 1 && (
            <>
              <button onClick={handleReady} disabled={game.currentPlayer.isReady || game.currentPlayer.draftingArea.length > 0}>
                {game.currentPlayer.isReady ? "Ready" : "Set Ready"}
              </button>
              {game.currentPlayer.draftingArea.length > 0 && (
                <p>You must move all cards from your drafting area before setting ready.</p>
              )}
            </>
          )}
        </div>
      )}

      {game.gameState === 1 && game.currentPhase === 2 && renderProductionPhase()}
      
      <div className="other-players">
        <h3>Other Players</h3>
        {game.otherPlayers.map((player) => (
          <div key={player.id} className="player">
            <PlayerInfo player={player} />
            <DraftingArea 
              player={player} 
              currentPlayerId={game.currentPlayer.id}
              handleMoveToConstruction={handleMoveToConstruction}
              handleDiscard={handleDiscard}
            />
            <ConstructionArea 
              player={player}
              currentPlayerId={game.currentPlayer.id}
              handleAddResource={handleAddResource}
            />
            <Empire player={player} currentPlayerId={game.currentPlayer.id} />
          </div>
        ))}
      </div>
    </div>
  );
};

export default GameBoard;
