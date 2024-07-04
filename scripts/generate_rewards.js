// Shuffle the array of unique item_definitions (spriteids) for variety
function shuffleArray(array) {
  for (let i = array.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [array[i], array[j]] = [array[j], array[i]];
  }
}

const description = `Daglig gave for at besøge Habbo Hotel.`;
// List of possible unique item_definitions (also used as spriteids) for special items (Friday or Saturday)
const uniqueItemDefinitionsList = [
  '716',
  '1995',
  '350',
  '326',
  '327',
  '328',
  '331',
  '309',
  '759',
  '333',
  // Add more unique items as needed
];

// List of possible item_definitions (also used as spriteids) for regular items
const itemDefinitionsList = [
  '209',
  '212',
  '71,212',
  '73,209',
  '1613,209',
  '1614,212',
  '1620,209',
  '1621,212',
  '1622,209',
  '1623,212',
  // Add more regular items as needed
];

// Shuffle the array of unique item_definitions (spriteids) for special items for variety
shuffleArray(uniqueItemDefinitionsList);

// Shuffle the array of item_definitions (spriteids) for regular items for variety
shuffleArray(itemDefinitionsList);

// Date range
const startDate = new Date('2024-07-03');
const endDate = new Date('2024-10-10');

// Function to generate a random date within the date range
function getRandomDate(startDate, endDate) {
  const diffInMilliseconds = endDate - startDate;
  const randomMilliseconds = Math.floor(Math.random() * diffInMilliseconds);
  return new Date(startDate.getTime() + randomMilliseconds);
}

// Counter to keep track of the next unique item_definition (spriteid) for special items
let nextUniqueItemDefinitionIndex = 0;

// Generate and print random INSERT statements within the date range
while (startDate <= endDate) {
  const isFridayOrSaturday = startDate.getDay() === 6; /* Friday */ //||
  // startDate.getDay() === 6; /* Saturday */

  if (isFridayOrSaturday) {
    // Check if there are remaining unique item_definitions (spriteids) for special items to use
    if (nextUniqueItemDefinitionIndex < uniqueItemDefinitionsList.length) {
      const specialItemDefinition =
        uniqueItemDefinitionsList[nextUniqueItemDefinitionIndex];
      nextUniqueItemDefinitionIndex++;

      const specialAvailableFrom = startDate;
      const specialAvailableTo = new Date(
        startDate.getTime() + 24 * 60 * 60 * 1000
      ); // 1 day

      // Generate the SQL INSERT statement for the special item with the unique item_definition (spriteid)
      const specialInsertStatement = `
        INSERT INTO rewards (item_definitions, available_from, available_to, description, required_streak)
        VALUES ('${specialItemDefinition}', '${specialAvailableFrom
        .toISOString()
        .slice(0, 19)}', '${specialAvailableTo
        .toISOString()
        .slice(0, 19)}', '${description}', 0);
      `;

      console.log(specialInsertStatement);
    } else {
      const itemDefinition =
        itemDefinitionsList[
          Math.floor(Math.random() * itemDefinitionsList.length)
        ];

      const availableFrom = startDate;
      const availableTo = new Date(startDate.getTime() + 24 * 60 * 60 * 1000); // 1 day

      // Generate the SQL INSERT statement for a regular item with an item_definition (spriteid)
      const insertStatement = `
        INSERT INTO rewards (item_definitions, available_from, available_to, description, required_streak)
        VALUES ('${itemDefinition}', '${availableFrom
        .toISOString()
        .slice(0, 19)}', '${availableTo
        .toISOString()
        .slice(0, 19)}', '${description}', 0);
      `;

      console.log(insertStatement);
    }
  } else {
    const itemDefinition =
      itemDefinitionsList[
        Math.floor(Math.random() * itemDefinitionsList.length)
      ];

    const availableFrom = startDate;
    const availableTo = new Date(startDate.getTime() + 24 * 60 * 60 * 1000); // 1 day

    // Generate the SQL INSERT statement for a regular item with an item_definition (spriteid)
    const insertStatement = `
        INSERT INTO rewards (item_definitions, available_from, available_to, description, required_streak)
        VALUES ('${itemDefinition}', '${availableFrom
      .toISOString()
      .slice(0, 19)}', '${availableTo
      .toISOString()
      .slice(0, 19)}', '${description}', 0);
      `;

    console.log(insertStatement);
  }

  // Move to the next day
  startDate.setDate(startDate.getDate() + 1);
}
