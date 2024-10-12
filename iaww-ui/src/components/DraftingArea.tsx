import React from 'react';
import { PlayerStatus } from '../types';
import CardDetails from './CardDetails';

interface DraftingAreaProps {
  player: PlayerStatus;
  currentPlayerId: string;
  handleMoveToConstruction: (cardId: string) => void;
  handleDiscard: (cardId: string) => void;
}

const DraftingArea: React.FC<DraftingAreaProps> = ({ 
  player, 
  currentPlayerId, 
  handleMoveToConstruction, 
  handleDiscard 
}) => {
  return (
    <div className="drafting-area">
      <h3>{player.name}'s Drafting Area</h3>
      {player.draftingArea.map((card) => (
        <div key={card.id} className="card">
          <CardDetails card={card} />
          {player.id === currentPlayerId && (
            <div className="card-actions">
              <button onClick={() => handleMoveToConstruction(card.id)}>Move to Construction</button>
              <button onClick={() => handleDiscard(card.id)}>Discard</button>
            </div>
          )}
        </div>
      ))}
    </div>
  );
};

export default DraftingArea;