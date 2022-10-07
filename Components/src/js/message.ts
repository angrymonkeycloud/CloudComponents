//import { CoreString } from "./string";
//import { CorePopup } from "./popup";

//export class CoreMessage {
//  // Properties

//  private static get selector(): string {
//    return "body > .popup .message";
//  }

//  private static get element(): JQuery<HTMLElement> {
//    return $(this.selector);
//  }

//  static get isOpened(): boolean {
//    return $(this.selector).length > 0;
//  }

//  // Methods

//  static open(
//    title: string | HTMLElement,
//    description: string | HTMLElement,
//    buttons: CoreMessageButton[]
//  ): void {
//    let buttonsHtml = "";

//    buttons.forEach(function(button, index) {
//      let id = CoreString.randomLetters(20);

//      let buttonElement = $(
//        `<button id='` +
//          id +
//          `' class="message-button">` +
//          button.content +
//          `</button>`
//      );

//      if (button.method !== undefined)
//        $(document).on("click", "#" + id, button.method);

//      $(document).on("click", "#" + id, function() {
//        CoreMessage.close();
//      });

//      buttonsHtml += buttonElement[0].outerHTML;
//    });

//    let html =
//      `
//            <div class="message">
//                <div class="message-content">
//                    <p class="message-title">` +
//      title +
//      `</p>
//                    <p class="message-description">` +
//      description +
//      `</p>
//                    <div class="message-buttons">` +
//      buttonsHtml +
//      `</div>
//                </div>
//            </div>`;

//    CorePopup.open(html,{
//       preventClose: false
//    });
//    CoreMessage.element
//      .find(".message-button")
//      .first()
//      .focus();
//  }

//  static close(): void {
//    CorePopup.close();
//  }

//  static click(): void {
//    this.element.find(".message-button:focus").click();
//  }

//  static selectPreviousButton(): void {
//    let selected = this.element.find(".message-button:focus");

//    if (selected.length === 0) {
//      CoreMessage.element
//        .find(".message-button")
//        .first()
//        .focus();
//      return;
//    }

//    let previous = selected.not(":hidden").prev();

//    if (previous.length > 0) previous.focus();
//  }

//  static selectNextButton(): void {
//    let selected = this.element.find(".message-button:focus");

//    if (selected.length === 0) {
//      this.element
//        .find(".message-button")
//        .first()
//        .focus();
//      return;
//    }

//    let next = selected.not(":hidden").next();

//    if (next.length > 0) next.focus();
//  }
//}

//export class CoreMessageButton {
//  constructor(
//    public content: string | HTMLElement,
//    public method?: () => void
//  ) {}
//}

//// On Keyboard Down
//$(document).on("keydown", document, function(eventObject) {
    
//  if (CoreMessage.isOpened)
//    switch (eventObject.keyCode) {
//      case 13: // Enter
//        CoreMessage.click();
//        break;

//      case 27: // Escape
//        CoreMessage.close();
//        break;

//      case 37: // Left Arrow
//        CoreMessage.selectPreviousButton();
//        break;

//      case 39:
//        CoreMessage.selectNextButton();
//        break;

//      default:
//        break;
//    }
//});
