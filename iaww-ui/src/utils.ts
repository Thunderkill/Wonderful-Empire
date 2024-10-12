export const getCardTypeString = (type: number): string => {
  const types = ['Materials', 'Energy', 'Science', 'Gold', 'Exploration'];
  return types[type] || 'Unknown';
};

export const getResourceTypeString = (type: number): string => {
  const types = ['Materials', 'Energy', 'Science', 'Gold', 'Exploration', 'Krystallium'];
  return types[type] || 'Unknown';
};

export const getResourceColor = (type: number): string => {
  const colors = ['#ada594', '#343433', '#59a430', '#c4b822', '#3189c3', '#a71111'];
  return colors[type] || '#000000';
};

export const getResourceTypeNumber = (resourceName: string): number => {
  const resourceTypes = ['Materials', 'Energy', 'Science', 'Gold', 'Exploration', 'Krystallium'];
  return resourceTypes.indexOf(resourceName);
};