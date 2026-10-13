# Future work

- OpenAI Genie: add an OpenAI Responses API provider for chat, intent handling and widget generation. Reuse Context Engine, chat history, MIG.Audio and shared HomeGenie tools with the Gemini providers. Start with chat/device control, then widget generation and editing; keep API credentials on the backend.
- YoT: add RTL layout support for languages written from right to left.

- Audio service groups: exclude Audio.VoiceRecognizer and Audio.VoiceSynthesizer from automatic control-group creation while preserving module registration and address-based events. Allow manual grouping and retain existing groups, which may have been customized. Start with a targeted domain exclusion; consider a general interface capability to opt out of automatic grouping later.
