const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
const months = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

class TimeClock extends ControllerInstance {

  onInit() {
    this.model(this.refreshFn);
  }

  refreshFn(element, field, view, refreshCallback) {
    const now = new Date();
    switch (field) {

      case 'date':
        const month = months[now.getMonth()];
        element.html(month + ' ' + now.getDate() + ', ' + now.getFullYear());
        break;

      case 'time':
        element.html(now.toLocaleTimeString());
        break;

      case 'info':
        const day = days[now.getDay()];
        element.html(day);
        break;

    }
    refreshCallback(1000);
  }

}
