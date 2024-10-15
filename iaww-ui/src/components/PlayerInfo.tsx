import React from 'react';
import { PlayerStatus } from '../types';
import { getResourceColor, getResourceTypeNumber } from '../utils';

interface PlayerInfoProps {
  player: PlayerStatus;
  isCurrentPlayer: boolean;
}

const PlayerInfo: React.FC<PlayerInfoProps> = ({ player, isCurrentPlayer }) => {
  return (
    <div className="player-info">
      <h3>{isCurrentPlayer ? "Your Information" : `${player.name}'s Information`}</h3>
      <p>Resources:</p>
      <ul>
        {Object.entries(player.resources).map(([resource, amount]) => (
          <li key={resource} style={{color: getResourceColor(getResourceTypeNumber(resource))}}>
            {resource}: {amount}
          </li>
        ))}
      </ul>
      <p>Discarded Resource Pool: {player.discardedResourcePool}</p>
      <p>Hand Count: {player.handCount}</p>
      <p>Ready: {player.isReady ? "Yes" : "No"}</p>
      <p>Has Drafted: {player.hasDraftedThisRound ? "Yes" : "No"}</p>
    </div>
  );
};

export default PlayerInfo;
