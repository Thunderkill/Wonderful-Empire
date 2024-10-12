export interface Card {
  id: string;
  name: string;
  type: number;
  constructionCost: { [key: string]: number };
  production: { [key: string]: number };
  victoryPoints: number;
  recyclingBonus: number;
  specialAbility: number;
  investedResources: { [key: string]: number };
}

export interface PlayerStatus {
  id: string;
  name: string;
  resources: { [key: string]: number };
  hand: Card[];
  handCount: number;
  draftingArea: Card[];
  constructionArea: Card[];
  empire: Card[];
  isReady: boolean;
  hasDraftedThisRound: boolean;
}

export interface GameStatus {
  gameId: string;
  gameState: number;
  currentPhase: number;
  currentRound: number;
  host: PlayerStatus;
  currentPlayer: PlayerStatus;
  otherPlayers: PlayerStatus[];
  winnerId?: string;
  finalScores?: { [playerId: string]: number };
}