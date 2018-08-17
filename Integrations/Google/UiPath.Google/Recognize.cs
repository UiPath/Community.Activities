using Google.Apis.Auth.OAuth2;
using Google.Cloud.Speech.V1;
using Google.Cloud.TextToSpeech.V1;
using Google.Protobuf;
using Grpc.Auth;
using NAudio.Wave;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Google.Cloud.Speech.V1.SpeechClient;

namespace UiPath.Google
{
    public static class Recognize
    {
        static object writeLock;
        static bool writeMore;
        static WaveInEvent waveIn;
        static StreamingRecognizeStream streamingCall;
        public static string text;

        public static async void StartRecordingAsync(double confidence, string language, string serviceAcc)
        {
            writeLock = new object();
            writeMore = true;
            waveIn = new WaveInEvent();
            streamingCall = null;
            text = default(String);

            if (WaveIn.DeviceCount < 1)
            {
                throw new Exception("No microphone!");
            }
            var speech = default(SpeechClient);
            try
            {
                GoogleCredential googleCredential;
                using (Stream m = new FileStream(serviceAcc, FileMode.Open))
                    googleCredential = GoogleCredential.FromStream(m);
                var channel = new Grpc.Core.Channel(SpeechClient.DefaultEndpoint.Host,
                    googleCredential.ToChannelCredentials());
                speech = SpeechClient.Create(channel);
            }
            catch (Exception e)
            {
                throw e;
            }
            streamingCall = speech.StreamingRecognize();
            // Write the initial request with the config.
            try
            {
                await streamingCall.WriteAsync(
                    new StreamingRecognizeRequest()
                    {
                        StreamingConfig = new StreamingRecognitionConfig()
                        {
                            Config = new RecognitionConfig()
                            {
                                Encoding =
                                RecognitionConfig.Types.AudioEncoding.Linear16,
                                SampleRateHertz = 16000,
                                LanguageCode = language,
                            },
                            InterimResults = true,
                        }
                    });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            // Print responses as they arrive.
            Task printResponses = Task.Run(async () =>
            {
                while (await streamingCall.ResponseStream.MoveNext(
                    default(CancellationToken)))
                {
                    foreach (var result in streamingCall.ResponseStream
                        .Current.Results)
                    {
                        foreach (var alternative in result.Alternatives)
                        {
                            if (alternative.Confidence > confidence)
                            {
                                text = alternative.Transcript;
                                Console.WriteLine(text);
                            }
                            else
                            {
                                text = String.Empty;
                            }
                        }
                    }
                }
            });

            // Read from the microphone and stream to API.
            waveIn.DeviceNumber = 0;
            waveIn.WaveFormat = new WaveFormat(16000, 1);
            waveIn.DataAvailable +=
                (object sender, WaveInEventArgs args) =>
                {
                    lock (writeLock)
                    {
                        if (!writeMore) return;
                        try
                        {
                            streamingCall.WriteAsync(
                            new StreamingRecognizeRequest()
                            {
                                AudioContent = ByteString
                                    .CopyFrom(args.Buffer, 0, args.BytesRecorded)
                            }).Wait();
                        }
                        catch (Exception e)
                        {
                            throw e.InnerException;
                        }

                    }
                };
            waveIn.StartRecording();
            Console.WriteLine("Speak now.");
            return;
        }

        public static async Task<string> StopRecording(double confidence)
        {
            // Stop recording and shut down.
            waveIn.StopRecording();
            lock (writeLock) writeMore = false;
            await streamingCall.WriteCompleteAsync();
            return text;
        }

        public static void TextToSpeech(string text, string languageCode, SsmlVoiceGender gender, string serviceAcc)
        {
            GoogleCredential credentials = GoogleCredential.FromFile(serviceAcc);

            TextToSpeechClient client = TextToSpeechClient.Create(credentials);

            SynthesizeSpeechResponse response = client.SynthesizeSpeech(
                new SynthesisInput()
                {
                    Text = text
                },
                new VoiceSelectionParams()
                {
                    LanguageCode = languageCode,
                    SsmlGender = gender
                },
                new AudioConfig()
                {
                    AudioEncoding = AudioEncoding.Linear16
                }
            );

            string speechFile = Path.Combine(Directory.GetCurrentDirectory(), "sample.wav");

            File.WriteAllBytes(speechFile, response.AudioContent);
            System.Media.SoundPlayer player = new System.Media.SoundPlayer();

            player.SoundLocation = speechFile;
            player.PlaySync();
            try
            {
                File.Delete(speechFile);
            }
            catch
            {
                Console.WriteLine("Cannot delete the file");
            }
        }

    }
}
