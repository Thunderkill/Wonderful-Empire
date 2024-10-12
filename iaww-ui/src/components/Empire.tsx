import React from 'react';
import { PlayerStatus, Card } from '../types';
import { getResourceColor, getResourceTypeNumber } from '../utils';

interface EmpireProps {
  player: PlayerStatus;
}

const Empire: React.FC<EmpireProps> = ({ player }) => {
  const renderProduction = (card: Card) => {
    return (
      <div className="production">
        <h5>Production:</h5>
        {Object.entries(card.production).map(([resource, amount]) => (
          <p key={resource} style={{color: getResourceColor(getResourceTypeNumber(resource))}}>
            {resource}: {amount}
          </p>
        ))}
      </div>
    );
  };

  return (
    <div className="empire">
      <h3>{player.name}'s Empire (Constructed Buildings)</h3>
      {player.empire.map((card) => (
        <div key={card.id} className="empire-card">
          <h4>{card.name}</h4>
          <p>Victory Points: {card.victoryPoints}</p>
          {renderProduction(card)}
        </div>
      ))}
    </div>
  );
};

export default Empire;