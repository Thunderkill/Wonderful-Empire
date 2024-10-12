import React from 'react';
import { PlayerStatus, Card } from '../types';
import CardDetails from './CardDetails';
import { getResourceColor, getResourceTypeNumber } from '../utils';

interface ConstructionAreaProps {
  player: PlayerStatus;
  currentPlayerId: string;
  handleAddResource: (cardId: string, resourceType: number) => void;
}

const ConstructionArea: React.FC<ConstructionAreaProps> = ({ 
  player, 
  currentPlayerId, 
  handleAddResource 
}) => {
  const renderInvestedResources = (card: Card) => {
    return (
      <div className="invested-resources">
        <h5>Construction Progress:</h5>
        {Object.entries(card.constructionCost).map(([resource, cost]) => {
          const invested = card.investedResources[resource] || 0;
          const percentage = Math.min((invested / cost) * 100, 100);
          return (
            <div key={resource} className="resource-progress">
              <span style={{color: getResourceColor(getResourceTypeNumber(resource))}}>{resource}: </span>
              <div className="progress-bar">
                <div className="progress" style={{ width: `${percentage}%`, backgroundColor: getResourceColor(getResourceTypeNumber(resource)) }}></div>
              </div>
              <span>{invested}/{cost}</span>
            </div>
          );
        })}
      </div>
    );
  };

  const renderResourceButtons = (card: Card) => {
    return (
      <div className="resource-buttons">
        {Object.entries(card.constructionCost).map(([resource, cost]) => (
          <button
            key={resource}
            onClick={() => handleAddResource(card.id, getResourceTypeNumber(resource))}
            disabled={player.resources[resource] < 1 || (card.investedResources[resource] || 0) >= cost}
            style={{backgroundColor: getResourceColor(getResourceTypeNumber(resource))}}
          >
            Add {resource}
          </button>
        ))}
      </div>
    );
  };

  return (
    <div className="construction-area">
      <h3>{player.name}'s Construction Area</h3>
      {player.constructionArea.map((card) => (
        <div key={card.id} className="card">
          <CardDetails card={card} />
          {renderInvestedResources(card)}
          {player.id === currentPlayerId && renderResourceButtons(card)}
        </div>
      ))}
    </div>
  );
};

export default ConstructionArea;