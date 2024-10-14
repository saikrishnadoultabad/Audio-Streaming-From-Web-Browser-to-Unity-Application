using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Unity.RenderStreaming;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Windows;
using Whisper.Utils;
using Button = UnityEngine.UI.Button;
using Debug = UnityEngine.Debug;
using Toggle = UnityEngine.UI.Toggle;

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using System.Reflection;
using System.IO;


namespace Whisper.Samples
{
    using InputSystem = UnityEngine.InputSystem.InputSystem;

    static class InputReceiverExtension
    {
        public static void CalculateInputRegion(this InputReceiver reveiver, Vector2Int size)
        {
            reveiver.CalculateInputRegion(size, new Rect(0, 0, Screen.width, Screen.height));
        }
    }

    static class InputActionExtension
    {
        public static void AddListener(this InputAction action, Action<InputAction.CallbackContext> callback)
        {
            action.started += callback;
            action.performed += callback;
            action.canceled += callback;
        }
    }

    /// <summary>
    /// Record audio clip from microphone and make a transcription.
    /// </summary>
    public class MicrophoneDemo : MonoBehaviour
    {
        public WhisperManager whisper;
        public MicrophoneRecord microphoneRecord;
        public bool streamSegments = true;
        public bool printLanguage = true;

        [Header("UI")]
        public Button button;
        public Text buttonText;
        public Text outputText;
        public Text timeText;
        public Dropdown languageDropdown;
        public Toggle translateToggle;
        public Toggle vadToggle;
        public ScrollRect scroll;

        private string _buffer;
        private AudioClip audioClip;



#pragma warning disable 0649
        [SerializeField] public AudioStreamReceiver receiveAudioViewer;
        [SerializeField] public AudioSource receiveAudioSource;
        [SerializeField] public VideoStreamSender videoStreamSender;
        [SerializeField] private InputReceiver inputReceiver;

#pragma warning restore 0649
        private bool IsRecording = false;
        private List<float> audioData = new List<float>();
        private int totalSamplesRecorded = 0;

        private void Awake()
        {
            whisper.OnNewSegment += OnNewSegment;
            whisper.OnProgress += OnProgressHandler;

            microphoneRecord.OnRecordStop += OnRecordStop;

            button.onClick.AddListener(OnButtonPressed);
            languageDropdown.value = languageDropdown.options
                .FindIndex(op => op.text == whisper.language);
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

            translateToggle.isOn = whisper.translateToEnglish;
            translateToggle.onValueChanged.AddListener(OnTranslateChanged);

            vadToggle.isOn = microphoneRecord.vadStop;
            vadToggle.onValueChanged.AddListener(OnVadChanged);

            receiveAudioViewer.targetAudioSource = receiveAudioSource;


            inputReceiver.OnStartedChannel += OnStartedChannel;
        }


        private void OnStartedChannel(string connectionId)
        {
            CalculateInputRegion();
        }

        private void CalculateInputRegion()
        {
            if (!inputReceiver.IsConnected)
                return;
            var width = (int)(videoStreamSender.width / videoStreamSender.scaleResolutionDown);
            var height = (int)(videoStreamSender.height / videoStreamSender.scaleResolutionDown);
            inputReceiver.CalculateInputRegion(new Vector2Int(width, height));
            inputReceiver.SetEnableInputPositionCorrection(true);
        }

        private void OnButtonPressed()
        {
            if (!IsRecording)
            {
                IsRecording = true;
                buttonText.text = "Stop";
            }
            else
            {
                IsRecording = false;
                SaveAudioData();
                buttonText.text = "Record";
            }
        }
        void OnAudioFilterRead(float[] data, int channels)
        {
            if (IsRecording)
            {
                Debug.Log("OnAudioFilterRead called with " + data.Length + " samples and " + channels + " channels.");

                audioData.AddRange(data);
                totalSamplesRecorded += data.Length;
            }
        }

        void SaveAudioData()
        {
            AudioChunk chunk = new AudioChunk();
            chunk.Frequency = AudioSettings.outputSampleRate; // Sample rate
            chunk.Channels = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2; // Mono audio
            chunk.Length = totalSamplesRecorded / (float)chunk.Frequency;
            chunk.IsVoiceDetected = true; // Implement your voice detection logic here

            // Convert List<float> to float[]
            chunk.Data = audioData.ToArray();
            string path = Path.Combine(Application.persistentDataPath, "recordedAudio.wav");
            int sampleRate = chunk.Frequency;
            int channels = chunk.Channels;
            byte[] wavData = ConvertToWav(audioData.ToArray(), sampleRate, channels);
            System.IO.File.WriteAllBytes(path, wavData);
            Debug.Log("WAV file saved at: " + path);
            audioData.Clear();
        }

        private byte[] ConvertToWav(float[] audioData, int sampleRate, int channels)
        {
            int sampleCount = audioData.Length;
            int byteCount = sampleCount * sizeof(float);

            // WAV header
            byte[] header = new byte[44];
            System.Buffer.BlockCopy(System.Text.Encoding.ASCII.GetBytes("RIFF"), 0, header, 0, 4);
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(36 + byteCount), 0, header, 4, 4);
            System.Buffer.BlockCopy(System.Text.Encoding.ASCII.GetBytes("WAVE"), 0, header, 8, 4);
            System.Buffer.BlockCopy(System.Text.Encoding.ASCII.GetBytes("fmt "), 0, header, 12, 4);
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(16), 0, header, 16, 4);
            System.Buffer.BlockCopy(System.BitConverter.GetBytes((short)3), 0, header, 20, 2); // IEEE float format
            System.Buffer.BlockCopy(System.BitConverter.GetBytes((short)channels), 0, header, 22, 2);
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(sampleRate), 0, header, 24, 4);
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(sampleRate * channels * sizeof(float)), 0, header, 28, 4);
            System.Buffer.BlockCopy(System.BitConverter.GetBytes((short)(channels * sizeof(float))), 0, header, 32, 2);
            System.Buffer.BlockCopy(System.BitConverter.GetBytes((short)(8 * sizeof(float))), 0, header, 34, 2);
            System.Buffer.BlockCopy(System.Text.Encoding.ASCII.GetBytes("data"), 0, header, 36, 4);
            System.Buffer.BlockCopy(System.BitConverter.GetBytes(byteCount), 0, header, 40, 4);

            // Convert audio data to byte array
            byte[] wavData = new byte[44 + byteCount];
            System.Buffer.BlockCopy(header, 0, wavData, 0, 44);
            byte[] floatBytes = new byte[audioData.Length * sizeof(float)];
            Buffer.BlockCopy(audioData, 0, floatBytes, 0, floatBytes.Length);
            System.Buffer.BlockCopy(floatBytes, 0, wavData, 44, floatBytes.Length);

            return wavData;
        }

        

        private void OnNewSegment(WhisperSegment segment)
        {
            if (!streamSegments || !outputText)
                return;

            _buffer += segment.Text;
            outputText.text = _buffer + "...";
            UiUtils.ScrollDown(scroll);
        }
    }

}
