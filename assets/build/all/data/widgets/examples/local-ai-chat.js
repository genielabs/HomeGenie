/**
 * Controller for the Local AI Chat widget.
 * This class manages the user interface, handles user input, processes streaming
 * responses from a Large Language Model (LLM), and renders Markdown.
 */
class LocalAiChat extends ControllerInstance {
  // Widget Settings
  settings = {
    moduleSelect: {
      // In the widget settings dialog
      // show only modules with this field
      fieldFilter: 'LLM.TokenStream'
    },
    sizeOptions: ['medium', 'big']
  };

  currentAiMessageElement = null;
  currentAiTextBuffer = '';
  streamEndTimeout = null;
  isFirstToken = true;
  markedLib = null;
  textNoBoundModule = 'No module bound. Open widget settings to bind an LLM module.'
  renderUpdateTimer = null;
  isStreaming = false;

  /**
   * `onInit` lifecycle hook, called before the component's view is created.
   * Used here to start loading external libraries.
   */
  onInit() {
    // Load 'marked' library from local assets folder
    zuix.using('script', './assets/js/marked.min.js', () => {
      this.markedLib = window.marked;
    });
    // create `@translate` handler
    zuix.store('handlers')['translate'] = ($view, $el, lastResult, refreshCallback) => {
      const tag = $el.attr('@translate');
      this.translate('$ai_chat.' + tag).subscribe((tr) => {
        if (tr.indexOf('.$ai_chat.') === -1){
          $el.html(tr);
        }
      });
      //refreshCallback(lastResult);
    };
  }

  /**
   * `onCreate` lifecycle hook, called after the component's view is created and ready.
   * Used for initial setup, event handling, and data subscriptions.
   */
  onCreate() {
    const self = this;
    // Expose methods to be callable from the component's view (HTML).
    const instance = {
      sendMessage: this.sendMessage,
      handleKeyDown: this.handleKeyDown,
      module: this.boundModule || null,
      control(cmd) {
        self.boundModule?.control(cmd);
      },
      isStreaming() {
        return self.isStreaming;
      },
      isModelStarting() {
        return self.boundModule && instance.errorStatus() === '' && instance.getDownloadStatus() === 'Complete' && instance.initStatus() !== 'Ready';
      },
      configure() {
        self.showSettings();
      },
      initStatus() {
        return self.boundModule?.field('Status.Init')?.value || '';
      },
      errorStatus() {
        return self.boundModule?.field('Status.Error')?.value || '';
      },
      taskId() {
        return self.boundModule?.field('Status.Download.Id')?.value || '';
      },
      modelId() {
        return self.boundModule?.field('Status.Download.Task')?.value.split('/').pop() || '';
      },
      currentModel() {
        return self.boundModule?.field('LLM.Id')?.value || '';
      },
      getDownloadStatus() {
        return self.boundModule?.field('Status.Download')?.value || 'Complete';
      },
      getDownloadError() {
        return self.boundModule?.field('Status.Download.Error')?.value || '';
      },
      getDownloadProgress() {
        return +self.boundModule?.field('Status.Download.Progress')?.value || 0;
      },
      isDownloadingFile() {
        const status = self.boundModule?.field('Status.Download')?.value;
        return status === 'Requested' || status === 'Downloading' || status === 'Error' || status === 'Paused';
      }
    };
    this.declare(instance);

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

    // Subscribe to the token stream field.
    this.subscribe(tokenStream, (field) => {
      // Ignore tokens if we are not expecting an AI response.
      if (!this.currentAiMessageElement) return;

      const word = field.value;

      // On the first token of a new response, reset the buffer and set UI to streaming state.
      if (this.isFirstToken) {
        this.currentAiTextBuffer = '';
        this.isFirstToken = false;
        this.setStreaming();

        // Immediate render for the first token to reduce perceived latency
        this.currentAiTextBuffer += word;
        this.renderMarkdown(this.currentAiMessageElement, this.currentAiTextBuffer);
      } else {
        // Accumulate text
        this.currentAiTextBuffer += word;

        // Instead of rendering on every single token, we use a timer to limit 
        // updates to once every 50ms. This prevents CPU saturation on long responses.
        if (!this.renderUpdateTimer) {
          this.renderUpdateTimer = setTimeout(() => {
            this.renderMarkdown(this.currentAiMessageElement, this.currentAiTextBuffer);
            this.renderUpdateTimer = null;
          }, 50);
        }
      }

      // Reset the stream-end timeout.
      clearTimeout(this.streamEndTimeout);
      this.streamEndTimeout = setTimeout(() => {

        // STREAM FINISHED:
        // Ensure any pending throttled render is cancelled and force a final update
        // to guarantee the complete text is displayed.

        if (this.renderUpdateTimer) {
          clearTimeout(this.renderUpdateTimer);
          this.renderUpdateTimer = null;
        }
        this.renderMarkdown(this.currentAiMessageElement, this.currentAiTextBuffer);

        // Remove the 'streaming' class to hide the cursor.
        if (this.currentAiMessageElement) {
          this.currentAiMessageElement.classList.remove('streaming');
        }

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
    //    this.field('send_button').attr('disabled', null);
    this.field('prompt_input').attr('disabled', null);
  }

  disableInput() {
    //    this.field('send_button').attr('disabled', true);
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
    this.isStreaming = true;
  }
  setReady() {
    this.isStreaming = false;
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

      // Use 'true' to enable Smart Scroll (only scroll if user is already near bottom)
      this.scrollToBottom(true);
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

    if (!this.boundModule.isOnline) {
      utils.ui
        .tooltip('LLM module is not available at the moment.', { duration: 2000 });
      return;
    }

    // Set the UI to a 'processing' state and clear the input field.
    this.setProcessing();
    promptInput.value('');

    // Add the user's message to the chat history UI.
    this.addMessageToHistory('user', userInput);

    // Prepare for the AI's response:
    // 1. Initialize buffer as empty (Cursor is now handled via CSS).
    this.currentAiTextBuffer = '';

    // 2. Create the message element.
    this.currentAiMessageElement = this.addMessageToHistory('ai', this.currentAiTextBuffer);

    // 3. Add 'streaming' class to show the blinking cursor immediately.
    this.currentAiMessageElement.classList.add('streaming');

    this.isFirstToken = true;

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
    authorSpan.className = 'author label';
    authorSpan.textContent = (author === 'ai' ? 'AI Genie' : 'Tu');

    const textDiv = document.createElement('div');
    textDiv.className = 'text';

    // User messages are also rendered as Markdown for consistency (e.g., to show code snippets).
    if (author === 'user') {
      if (this.markedLib) {
        const html = this.markedLib.parseInline(text.trim());
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
   * @param {boolean} smartScroll If true, checks if user is reading history before scrolling.
   */
  scrollToBottom(smartScroll = false) {
    const chatHistoryElement = this.field('chat_history').get();
    if (!chatHistoryElement) return;

    if (smartScroll) {
      // Threshold in pixels to define "near bottom". 
      // Since we throttle updates, the new content chunk is usually small, 
      // so 150px is a safe buffer to detect if user is following the stream.
      const threshold = 150;

      // Calculate distance from bottom *after* content update.
      // Formula: Total Height - Scrolled Amount - Visible Height
      const distanceFromBottom = chatHistoryElement.scrollHeight - chatHistoryElement.scrollTop - chatHistoryElement.clientHeight;

      // If the distance is larger than the threshold, it means the user 
      // has actively scrolled up to read previous messages. Do not disturb them.
      if (distanceFromBottom > threshold) {
        return;
      }
    }

    // Force scroll to bottom (default behavior or if user is near bottom)
    setTimeout(() => {
      chatHistoryElement.scrollTop = chatHistoryElement.scrollHeight;
    });
  }

  /**
   * Fetches and applies translated strings for UI elements.
   */
  translateUI() {
    this.translate('$ai_chat.title').subscribe((tr) => {
      this.model().title = tr;
    });
    this.translate('$ai_chat.no_bound_module').subscribe((tr) => {
      this.textNoBoundModule = tr;
    });
    this.translate('$ai_chat.start_typing_prompt').subscribe((tr) => {
      this.field('prompt_input').get().placeholder = tr;
    });
  }
}
