function centerRewardItem() {
  const scrollableDiv = document.querySelector('.rewards-body');
  const listItem = document.querySelector('li.reward-item-current');

  // Check if the list item with the specified class exists
  if (!listItem) {
    console.error('List item with class "reward-item-current" not found');
    return;
  }

  // Calculate the position to scroll to
  const listItemPosition = listItem.offsetLeft;
  const listItemWidth = listItem.offsetWidth;
  const scrollableDivWidth = scrollableDiv.offsetWidth;
  const scrollTo = listItemPosition - (scrollableDivWidth / 2) + (listItemWidth / 2);

  // Scroll the div to center the list item
  scrollableDiv.scrollLeft = scrollTo;
}

document.addEventListener('DOMContentLoaded', function() {
  centerRewardItem();
});