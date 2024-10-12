import React from 'react';
import { Card } from '../types';
import { getCardTypeString, getResourceColor, getResourceTypeNumber, getResourceTypeString } from '../utils';

interface CardDetailsProps {
  card: Card;
}

const CardDetails: React.FC<CardDetailsProps> = ({ card }) => {
  const renderConstructionRequirements = () => {
    return (
      <div className="construction-requirements">
        <h5>Construction Requirements:</h5>
        {Object.entries(card.constructionCost).map(([resource, cost]) => (
          <p key={resource} style={{color: getResourceColor(getResourceTypeNumber(resource))}}>
            {resource}: {cost}
          </p>
        ))}
      </div>
    );
  };

  const renderProduction = () => {
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

  const renderRecyclingBonus = () => {
    return (
      <div className="recycling-bonus">
        <h5>Recycling Bonus:</h5>
        <p style={{color: getResourceColor(card.recyclingBonus)}}>
          {getResourceTypeString(card.recyclingBonus)}
        </p>
      </div>
    );
  };

  return (
    <div className="card-details">
      <h4>{card.name}</h4>
      <p>Type: {getCardTypeString(card.type)}</p>
      <p>Victory Points: {card.victoryPoints}</p>
      {renderConstructionRequirements()}
      {renderProduction()}
      {renderRecyclingBonus()}
    </div>
  );
};

export default CardDetails;