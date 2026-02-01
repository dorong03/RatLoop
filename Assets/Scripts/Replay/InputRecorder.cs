using System.Collections.Generic;
using UnityEngine;

public class InputRecorder : MonoBehaviour
{
    private Queue<InputFrame> recordingQueue = new Queue<InputFrame>();
    
    private bool isRecording = false;

    public void StartRecording()
    {
        recordingQueue.Clear();
        isRecording = true;
    }

    public void StopRecording()
    {
        isRecording = false;
    }

    public void Record(InputFrame inputFrame)
    {
        if (isRecording)
        {
            recordingQueue.Enqueue(inputFrame);
        }
    }

    public List<InputFrame> GetInputFrames()
    {
        return new List<InputFrame>(recordingQueue);
    }
}
