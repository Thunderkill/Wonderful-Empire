import React from 'react';
import { GameStatus } from '../types';
import { getResourceColor, getResourceTypeNumber } from '../utils';

interface EndGameScreenProps {
  game: GameStatus;
  navigate: (path: string) => void;
}

const EndGameScreen: React.FC<EndGameScreenProps> = ({ game, navigate }) => {
  const allPlayers = [game.currentPlayer, ...game.otherPlayers];
  const sortedPlayers = allPlayers.sort((a, b) => {
    const scoreA = game.finalScores?.[a.id] || 0;
    const scoreB = game.finalScores?.[b.id] || 0;
    return scoreB - scoreA;
  });
  const winner = sortedPlayers.find(player => player.id === game.winnerId);

  return (
    <div className="end-game-screen">
      <h2>Game Over</h2>
      {winner && <h3>Winner: {winner.name}</h3>}
      <h4>Final Scores:</h4>
      <ul>
        {sortedPlayers.map((player, index) => (
          <li key={player.id}>
            {index + 1}. {player.name}: {game.finalScores?.[player.id] || 0} points
            {player.id === game.winnerId && " (Winner)"}
          </li>
        ))}
      </ul>
      <h4>Empire Summary:</h4>
      {sortedPlayers.map(player => (
        <div key={player.id} className="player-summary">
          <h5>{player.name}</h5>
          <p>Cards in Empire: {player.empire.length}</p>
          <p>Resources:</p>
          <ul>
            {Object.entries(player.resources).map(([resource, amount]) => (
              <li key={resource} style={{color: getResourceColor(getResourceTypeNumber(resource))}}>
                {resource}: {amount}
              </li>
            ))}
          </ul>
        </div>
      ))}
      <button onClick={() => navigate('/')}>Return to Lobby</button>
    </div>
  );
};

export default EndGameScreen;