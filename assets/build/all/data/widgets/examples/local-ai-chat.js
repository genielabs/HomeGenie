/**
 * Controller for the Local AI Chat widget.
 * This class manages the user interface, handles user input, processes streaming
 * responses from a Large Language Model (LLM), and renders Markdown.
 */
class LocalAiChat extends ControllerInstance {

  // --- PROPERTIES ---

  /**
   * Holds a reference to the HTML element containing the latest AI message.
   * This is used to progressively update the content during a streaming response.
   * @type {HTMLElement|null}
   */
  currentAiMessageElement = null;

  /**
   * A buffer to accumulate the raw text from the AI's streaming response.
   * @type {string}
   */
  currentAiTextBuffer = '';

  /**
   * A timeout used to detect the end of a streaming response.
   * It is reset with each new token and fires when tokens stop arriving.
   * @type {number|null}
   */
  streamEndTimeout = null;

  /**
   * A flag to identify the very first token of a new AI response,
   * used to reset the buffer and UI state.
   * @type {boolean}
   */
  isFirstToken = true;

  /**
   * Stores the `marked` library instance once it's loaded from the CDN.
   * @type {object|null}
   */
  markedLib = null;

  /**
   * Default UI message to show when no LLM module is bound to the widget.
   * This can be replaced by a translated string.
   * @type {string}
   */
  textNoBoundModule = 'No module bound. Open widget settings to bind an LLM module.'

  /**
   * `onInit` lifecycle hook, called before the component's view is created.
   * Used here to start loading external libraries.
   */
  onInit() {
    // Load 'marked' library from local assets folder
    zuix.using('script', './assets/js/marked.min.js', () => {
      this.markedLib = window.marked;
    });
  }

  /**
   * `onCreate` lifecycle hook, called after the component's view is created and ready.
   * Used for initial setup, event handling, and data subscriptions.
   */
  onCreate() {
    // Expose methods to be callable from the component's view (HTML).
    this.declare({
      sendMessage: this.sendMessage,
      handleKeyDown: this.handleKeyDown
    });

    // Load and apply translated strings to the UI.
    // (from `/assets/i18n/widgets/` folder)
    this.translateUI();

    // Set the initial UI state to 'ready'.
    this.setReady();

    // If no LLM module is bound, show an error message and disable input.
    if (!this.boundModule) {
      this.addMessageToHistory('ai', this.textNoBoundModule);
      this.disableInput();
      return;
    }

    // Set the widget title from the bound module's name.
    this.model().title = this.boundModule.name;

    // Find the field that will provide the streaming tokens.
    var tokenStream = this.boundModule.field('LLM.TokenStream');
    if (!tokenStream) return;

    // Subscribe to the token stream field. This callback will be executed
    // for each new token received from the LLM.
    this.subscribe(tokenStream, (field) => {
      // Ignore tokens if we are not expecting an AI response.
      if (!this.currentAiMessageElement) return;

      const word = field.value;

      // On the first token of a new response, reset the buffer and set UI to streaming state.
      if (this.isFirstToken) {
        this.currentAiTextBuffer = '';
        this.isFirstToken = false;
        this.setStreaming();
      }

      // Append the new token to the raw text buffer.
      this.currentAiTextBuffer += word;

      // Render the entire accumulated buffer as Markdown in the current AI message element.
      this.renderMarkdown(this.currentAiMessageElement, this.currentAiTextBuffer);

      // Reset the stream-end timeout. If no new token arrives within 1.5 seconds,
      // we'll consider the stream finished.
      clearTimeout(this.streamEndTimeout);
      this.streamEndTimeout = setTimeout(() => {
        this.setReady();
        // Return focus to the input field for a better user experience.
        this.field('prompt_input').get().focus();
      }, 1500);
    });

    // Get the Error status field
    var statusError = this.boundModule.field('Status.Error');
    if (statusError) {
        this.subscribe(statusError, (f) => {
            this.checkError(f, true);
        });
        this.checkError(statusError);
    }
  }

  // --- UI STATE MANAGEMENT METHODS ---

  enableInput() {
    this.field('send_button').attr('disabled', null);
    this.field('prompt_input').attr('disabled', null);
  }

  disableInput() {
    this.field('send_button').attr('disabled', true);
    this.field('prompt_input').attr('disabled', true);
  }
  setProcessing() {
    this.disableInput();
    this.field('arrow').hide();
    this.field('typing').hide();
    this.field('loading').show();
  }
  setStreaming() {
    this.disableInput();
    this.field('arrow').hide();
    this.field('loading').hide();
    this.field('typing').show();
  }
  setReady() {
    this.enableInput();
    this.field('typing').hide();
    this.field('loading').hide();
    this.field('arrow').show();
  }
  checkError(errorField, focus = false) {
      if (errorField.value !== '') {
        this.addMessageToHistory('ai', (`[ERROR] ${errorField.value}`));
        this.disableInput();
      } else {
        this.enableInput();
        if (focus) {
            this.field('prompt_input').get().focus();
        }
      }
  }

  /**
   * Renders a Markdown string into a target HTML element.
   * @param {HTMLElement} targetElement The container element for the rendered HTML.
   * @param {string} markdownText The raw Markdown string to render.
   */
  renderMarkdown(targetElement, markdownText) {
    if (this.markedLib && targetElement) {
      // Use the 'marked.parse()' method to convert the Markdown string into an HTML string.
      const html = this.markedLib.parse(markdownText);
      // Insert the resulting HTML into the '.text' child of the target element.
      targetElement.querySelector('.text').innerHTML = html;
      this.scrollToBottom();
    }
  }

  /**
   * Sends the user's input to the bound LLM module for processing.
   */
  sendMessage() {
    const promptInput = this.field('prompt_input');
    const userInput = promptInput.value().trim();

    // Do nothing if there's no input or no module bound.
    if (!userInput || !this.boundModule) {
      return;
    }

    // Set the UI to a 'processing' state and clear the input field.
    this.setProcessing();
    promptInput.value('');

    // Add the user's message to the chat history UI.
    this.addMessageToHistory('user', userInput);

    // Prepare for the AI's response:
    // 1. Add a placeholder message to the UI for the AI. A cursor '▌' is used as a visual cue.
    // 2. Initialize the text buffer and stream state.
    // 3. Store a reference to the newly created AI message element for progressive updates.
    this.currentAiTextBuffer = '▌';
    this.currentAiMessageElement = this.addMessageToHistory('ai', this.currentAiTextBuffer);
    this.isFirstToken = true;

    // Trigger the 'Process' control on the bound module, passing the user's input.
    this.boundModule.control('Process', userInput);
  }

  /**
   * A helper method to create and append a new message (from user or AI)
   * to the chat history container.
   * @param {'user'|'ai'} author The author of the message.
   * @param {string} text The message content (can be raw text or initial HTML).
   * @returns {HTMLElement} The newly created message container element.
   */
  addMessageToHistory(author, text) {
    const chatHistory = this.field('chat_history');

    const messageContainer = document.createElement('div');
    messageContainer.className = `message ${author}`;

    const authorSpan = document.createElement('span');
    authorSpan.className = 'author';
    authorSpan.textContent = (author === 'ai' ? 'AI Genie' : 'Tu');

    const textDiv = document.createElement('div');
    textDiv.className = 'text';

    // User messages are also rendered as Markdown for consistency (e.g., to show code snippets).
    if (author === 'user') {
      if (this.markedLib) {
        const html = this.markedLib.parse(text);
        textDiv.innerHTML = html;
      } else {
        textDiv.textContent = text;
      }
    } else {
      // AI messages start with raw text/HTML (like the cursor) and are updated later.
      textDiv.innerHTML = text;
    }

    messageContainer.appendChild(authorSpan);
    messageContainer.appendChild(textDiv);

    chatHistory.append(messageContainer);
    this.scrollToBottom();

    // Return the element so it can be stored and updated by the streaming logic.
    return messageContainer;
  }

  /**
   * Handles the 'keydown' event on the input field to send messages with Enter key.
   * @param {KeyboardEvent} event The keyboard event.
   */
  handleKeyDown(event) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  /**
   * Scrolls the chat history container to the bottom.
   */
  scrollToBottom() {
    const chatHistoryElement = this.field('chat_history').get();
    if (chatHistoryElement) {
      setTimeout(() => {
        chatHistoryElement.scrollTop = chatHistoryElement.scrollHeight;
      });
    }
  }

  /**
   * Fetches and applies translated strings for UI elements.
   */
  translateUI() {
    this.translate('$ai_chat.title').subscribe((tr) => {
      this.model().title = tr;
    });
    this.translate('$ai_chat.intro').subscribe((tr) => {
      this.model().intro = tr;
    });
    this.translate('$ai_chat.no_bound_module').subscribe((tr) => {
      this.textNoBoundModule = tr;
    });
    this.translate('$ai_chat.start_typing_prompt').subscribe((tr) => {
      this.field('prompt_input').get().placeholder = tr;
    });
  }
}
