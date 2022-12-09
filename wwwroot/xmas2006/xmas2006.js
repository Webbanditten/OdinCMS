function repositionPresentModal() {
  var calendar = document.getElementById("calendar");
  var calendarBounds = calendar.getBoundingClientRect();

  var presentModal = document.querySelector(".present-modal");
  presentModal.style.left =
    calendarBounds.left + presentModal.getBoundingClientRect().width / 3 + "px";
  presentModal.style.top =
    calendarBounds.top + presentModal.getBoundingClientRect().height / 3 + "px";
}

function showPresentModal(presentMarkup) {
    var presentModal = document.querySelector(".present-modal");
    var presentModalImages = presentModal.querySelector(".highlighted_items");
    var presentModalDescription = presentModal.querySelector(".description");
    presentModalImages.innerHTML = presentMarkup.querySelector(".images").innerHTML;
    presentModalDescription.innerHTML = presentMarkup.querySelector(".description").innerHTML;
    presentModal.style.display = "block";
    repositionPresentModal();

}
function hidePresentModal() {
    var presentModal = document.querySelector(".present-modal");
    presentModal.style.display = "none";
    repositionPresentModal();
}
function isPresentModalOpen() {
  var presentModal = document.querySelector(".present-modal");
  return presentModal.style.display !== "none";
}

function handleMouseMove(event) {
  var calendar = document.getElementById("calendar");
  var calendarInner = document.getElementById("calendar-inner");
  var calendarBounds = calendar.getBoundingClientRect();

  var hitBoxSize = 20;
  var topHitBox = calendarBounds.top + hitBoxSize;
  var leftHitBox = calendarBounds.left + hitBoxSize;
  var rightHitBox = calendarBounds.right - hitBoxSize;
  var bottomHitBox = calendarBounds.bottom - hitBoxSize;
  var scrollAmount = 2;
  var mousePosition = {
    x: event.clientX,
    y: event.clientY,
  };

  // is mouse inside calendar?
  if (
    mousePosition.x > calendarBounds.left &&
    mousePosition.x < calendarBounds.right &&
    mousePosition.y > calendarBounds.top &&
    mousePosition.y < calendarBounds.bottom
  ) {
    if (mousePosition.y < topHitBox) {
      calendar.style.cursor = "url(/xmas2006/images/pointer_up.png), pointer";
      if (calendar.scrollTop > 0) {
        calendar.style.cursor = "url(/xmas2006/images/pointer_up_active.png), pointer";
        calendar.scrollTop -= scrollAmount;
      }
    } else if (mousePosition.y > bottomHitBox) {
      calendar.style.cursor = "url(/xmas2006/images/pointer_down.png) 19 38, pointer";
      if (
        calendarInner.offsetHeight - calendar.offsetHeight !==
        calendar.scrollTop
      ) {
        calendar.style.cursor =
          "url(/xmas2006/images/pointer_down_active.png) 19 38, pointer";
        calendar.scrollTop += scrollAmount;
      }
    } else if (mousePosition.x < leftHitBox) {
      calendar.style.cursor = "url(/xmas2006/images/pointer_left.png) 0 0, pointer";
      if (calendar.scrollLeft > 0) {
        calendar.style.cursor =
          "url(/xmas2006/images/pointer_left_active.png) 0 0, pointer";
        calendar.scrollLeft -= scrollAmount;
      }
    } else if (mousePosition.x > rightHitBox) {
      calendar.style.cursor = "url(/xmas2006/images/pointer_right.png) 38 19, pointer";
      if (
        calendarInner.offsetWidth - calendar.offsetWidth !==
        calendar.scrollLeft
      ) {
        calendar.style.cursor =
          "url(/xmas2006/images/pointer_right_active.png) 38 19, pointer";
        calendar.scrollLeft += scrollAmount;
      }
    } else {
      calendar.style.cursor = "default";
    }
  }
}
function initCalendar() {
  var calendar = document.getElementById("calendar");
  var calendarBounds = calendar.getBoundingClientRect();
  calendar.scrollLeft = calendarBounds.width / 2;
  calendar.scrollTop = calendarBounds.height / 2;
  // Get mouse position
  calendar.onmousemove = handleMouseMove;

  var days = document.getElementsByClassName("day");
  // Iterate days
  for (var i = 0; i < days.length; i++) {
    var day = days[i];
    // Add click event
    day.onclick = function () {
      var present = this.getElementsByClassName("present")[0];
      var presentContent = this.getElementsByClassName("present_content")[0];
      //alert(presentContent.innerHTML);
      showPresentModal(presentContent);
    };
  }
  repositionPresentModal();
};

window.addEventListener("load", initCalendar);
window.addEventListener("onresize", repositionPresentModal);

var lastKnownScrollPosition = 0;
var ticking = false;

document.addEventListener('scroll', (e) => {
  lastKnownScrollPosition = window.scrollY;

  if (!ticking) {
    window.requestAnimationFrame(() => {
      repositionPresentModal();
      ticking = false;
    });

    ticking = true;
  }
});
