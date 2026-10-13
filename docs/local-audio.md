# Local speech recognition and synthesis

[Back to HomeGenie](../README.md)

HomeGenie integrates MIG.Audio 1.0.0 with two independently enabled interfaces:

| Interface | Engine | Purpose |
| --- | --- | --- |
| `Audio.VoiceRecognizer` | Whisper | Transcribe microphone recordings into text |
| `Audio.VoiceSynthesizer` | Piper | Generate spoken audio from text |

Speech is processed on the HomeGenie server. The browser captures the microphone
and plays synthesized audio. These features can be used with local or cloud AI
providers; choosing local speech does not change where the chat's language model runs.

## Enable and configure

Select local speech recognition and/or synthesis during initial setup, or enable
them later under **Settings → Configuration → Interfaces**. Changes apply
automatically. Options are shown only while the corresponding interface is enabled.

Recognition options include model, language (including automatic detection), and
processing threads. Synthesis options include voice, speed, and processing threads.
The language list uses supported UI languages compatible with Whisper; English-only
recognition models require English. CPU is the tested baseline and a GPU is not required.

Enabling an interface downloads missing model files automatically. An Internet
connection is needed for the download; installed models can then run locally.
Files are stored under `data/models/asr/` and `data/models/tts/` in the application's
working directory. The UI shows the relative model path, file status, and download
progress. Models have their own licenses; consult the selected model's source.

During a download, **Stop** pauses it. **Resume** continues a partial download,
and **Repeat download** starts it again. Disabling the interface stops its download
and hides its options. Re-enable it to access the download controls. An installed
file and a ready inference engine are separate states; the status icon's tooltip
provides further details.

## Use in AI chats

Open the chat options menu and select local recognition or synthesis when the
corresponding interface is available. Browser speech remains a separate choice.
The configured model, language, voice, and speed are used for local speech.

For recognition, start recording with the microphone control, then press **Stop**.
The recording is transcribed into the message editor. Review or correct the text
and press **Send** yourself; transcription does not send a message automatically.

Microphone access requires a secure browser context: **HTTPS or localhost**.
An ordinary HTTP LAN address cannot grant microphone access. When the browser is
on the server machine, use `http://localhost:<port>/`; remote clients need HTTPS.
The browser may also ask for microphone permission.

Local synthesis returns audio to the browser for playback, rather than playing
through the server's speakers. Browser playback restrictions may require a user
interaction before audio can start.

## HTTP API examples

Replace port `8080` with your HomeGenie service port (see `serviceaddress.txt`).
Enable the relevant interface before making requests. The examples use shell curl;
on Windows PowerShell use `curl.exe`.

```sh
# Transcribe a PCM16 WAV recording: mono, 16 kHz, little endian.
curl --data-binary @recording.wav -H 'Content-Type: audio/wav' \
  http://localhost:8080/api/Audio.VoiceRecognizer/chat-1/Voice.Transcribe/it

# Synthesize text using the configured voice and speed.
curl --data-binary 'Hello from HomeGenie.' \
  -H 'Content-Type: text/plain; charset=utf-8' \
  http://localhost:8080/api/Audio.VoiceSynthesizer/chat-1/Voice.Synthesize \
  --output response.wav

# Change speed for this request; Voice.Speak is an alias of Voice.Synthesize.
curl --data-binary 'Questa voce viene generata localmente.' \
  -H 'Content-Type: text/plain; charset=utf-8' \
  http://localhost:8080/api/Audio.VoiceSynthesizer/chat-1/Voice.Speak/1.15/it_IT-paola-medium \
  --output response.wav

# Inspect the model service.
curl http://localhost:8080/api/Audio.VoiceRecognizer/service/Model.Status
```

Recognition parameters are positional: `language/model`. Synthesis parameters
are `speed/voice/speaker`. Omitted parameters use configured defaults; `default`
can skip an earlier parameter. Recognition returns text in MIG `ResponseValue`;
synthesis returns binary WAV. Errors can be JSON even with HTTP 200, so clients
must validate the response before treating it as audio.

The request address (for example `chat-1`) determines the module receiving result
events, allowing separate chats to receive their own events. The reserved
`service` address exposes model status and download controls. Engine defaults
are shared by each interface rather than being separate engines per chat.

For the complete API, options, model management events, native runtime requirements,
and platform limitations, see the [MIG.Audio README](https://github.com/genielabs/mig-audio#readme).

## Service modules and groups

Voice recognition and synthesis modules remain registered and receive events at their request addresses. HomeGenie does not create control groups automatically for these two interfaces or add new audio modules to existing groups automatically. Existing groups are preserved, and users can still group audio modules manually. This keeps a fresh setup focused on device control groups.

## Troubleshooting

- If the microphone is unavailable, check HTTPS/localhost and browser permissions.
- If an engine is unavailable, enable its interface and inspect the model status
  and tooltip. Allow downloads and engine loading to finish before using speech.
- If a download is interrupted, re-enable the interface if needed and use Resume;
  Repeat download is available to restart a partial or installed model.
- If recognition is inaccurate, select the spoken language explicitly or a suitable
  model, and review the transcription before sending it.
- If local AI is slow, remember that speech and the language model share server
  resources. Processing threads and model sizes affect CPU and memory usage.
