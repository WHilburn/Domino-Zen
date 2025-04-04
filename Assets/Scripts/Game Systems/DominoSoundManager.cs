using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DominoSoundManager : MonoBehaviour
{
    public static DominoSoundManager Instance { get; private set; }
    
    [Header("User Settings")]
    public static float globalVolumeScale = 1f; // User-defined global volume scale modifier
    public DominoSoundType userSelectedSoundType = DominoSoundType.Click; // User-selected sound type

    [Header("Cascade Audio Settings")] //Create a separator in the inspector
    public AudioSource cascadeSource; // Background cascade sound
    public AudioClip cascadeClip;     // Rolling cascade sound
    public float maxVolume = .7f;  // Max volume when many dominoes are moving
    public float velocityScale = 0.01f; // Scale factor for volume adjustment
    public float volumeLerpSpeed = 2f; // Speed at which volume adjusts
    private float targetVolume = 0f; // The volume we want to reach
    public float totalVelocityThreshhold = -100f; // Minimum velocity to play the sound, total must exceed this
    public float minimumVelocity = 0.7f; // Minimum velocity for a piece to contibute to the cascase sound
    private float totalMovement = 0f; // Total movement of all dominoes
    
    [Header("Invididual Domino Sound Settings")]//Create a separator in the inspector
    public float minimumImpactForce = 1f;
    public float soundCooldown = 0.2f;
    private float lastSoundTime = 0f;
    private int songIndex = 0;
    private SongTitle currentSong = SongTitle.OdeToJoy;
    public int octaveOffset = 1;
    public DominoSoundList dominoPianoSounds;
    public DominoSoundList dominoClickSounds;

    public AudioClip dominoLockedSound;
    public AudioClip indicatorConfirmSound;

    [Header("AudioSource Pool Settings")]
    public int audioSourcePoolSize = 128; // Number of AudioSources in the pool
    private Queue<AudioSource> audioSourcePool;

    public enum SongTitle
    { 
        Twinkle, Entertainer, MapleLeafRag,
        MaryHadALittleLamb, HappyBirthday,
        OdeToJoy, TurkishMarch, FlightOfTheBumblebee, JingleBells, FurElise, 
        CanonInD, Beethoven5th, Greensleeves,
        MoonlightSonata, ClairDeLune, MinuteWaltz,
        ToccataAndFugue, WilliamTellOverture, SwanLake, BachPrelude
    
    }

    public enum DominoSoundType
    {
        Click,
        Piano
    }

    private static readonly Dictionary<string, int> noteMap = new Dictionary<string, int>
    {
        { "C3", 0 }, { "C#3", 1 }, { "Db3", 1 }, { "D3", 2 }, { "D#3", 3 }, { "Eb3", 3 }, { "E3", 4 },
        { "F3", 5 }, { "F#3", 6 }, { "Gb3", 6 }, { "G3", 7 }, { "G#3", 8 }, { "Ab3", 8 }, { "A3", 9 }, 
        { "A#3", 10 }, { "Bb3", 10 }, { "B3", 11 },

        { "C4", 12 }, { "C#4", 13 }, { "Db4", 13 }, { "D4", 14 }, { "D#4", 15 }, { "Eb4", 15 }, { "E4", 16 },
        { "F4", 17 }, { "F#4", 18 }, { "Gb4", 18 }, { "G4", 19 }, { "G#4", 20 }, { "Ab4", 20 }, { "A4", 21 }, 
        { "A#4", 22 }, { "Bb4", 22 }, { "B4", 23 },

        { "C5", 24 }, { "C#5", 25 }, { "Db5", 25 }, { "D5", 26 }, { "D#5", 27 }, { "Eb5", 27 }, { "E5", 28 },
        { "F5", 29 }, { "F#5", 30 }, { "Gb5", 30 }, { "G5", 31 }, { "G#5", 32 }, { "Ab5", 32 }, { "A5", 33 }, 
        { "A#5", 34 }, { "Bb5", 34 }, { "B5", 35 },

        { "-", -1 } // Rest note
    };


    private static readonly Dictionary<SongTitle, string> songLibrary = new Dictionary<SongTitle, string>
    {
        {SongTitle.FlightOfTheBumblebee, "B4C5D5C5B4A4G#4A4B4C5D5C5B4A4G#4A4B4A4G#4A4B4C5D5E5F5E5D5C5B4C5D5E5D5C5B4A4G#4A4B4C5D5E5F5G5A5G5F5E5D5C5B4A4G#4A4B4C5D5E5F5E5D5C5B4C5D5E5D5C5B4A4G#4A4B4A4G#4A4B4C5D5E5F5E5D5C5B4C5D5E5D5C5B4A4G#4A4B4C5D5E5F5G5A5G5F5E5D5C5B4A4G#4A4B4C5D5E5F5E5D5C5B4C5D5E5D5C5B4A4G#4A4B4A4G#4A4B4C5D5E5F5E5D5C5B4C5D5E5D5C5B4A4G#4A4B4C5D5E5F5G5A5G5F5E5D5C5B4A4G#4A4B4C5D5E5F5E5D5C5B4C5D5E5D5C5B4A4G#4A4"},
        {SongTitle.Twinkle,"C4C4G4G4A4A4G4-F4F4E4E4D4D4C4-G4G4F4F4E4E4D4-G4G4F4F4E4E4D4-C4C4G4G4A4A4G4-F4F4E4E4D4D4C4-"},
        {SongTitle.Entertainer,"E4G4C5A4G4-C4D4E4-G4A4C5-E4A4G4-"},
        {SongTitle.MapleLeafRag,"E4C4E4G4G4E4C4E4G4C5E4G4C4E4G4C5E4G4C5E4D4C4F4C4D4F4D4B3D4G4B3D4B3D4B3D4E4C4E4G4G4E4C4E4G4A4"},
        {SongTitle.MaryHadALittleLamb,"E4D4C4D4E4E4E4-D4D4D4-E4G4G4-"},
        {SongTitle.HappyBirthday,"G4G4A4G4C5B4-G4G4A4G4D5C5-"},
        {SongTitle.OdeToJoy,"E4E4F4G4G4F4E4D4-C4D4E4E4D4D4-E4E4F4G4G4F4E4D4-C4D4E4D4C4C4-D4E4F4C4D4E4F4E4D4C4G4-E4E4F4G4G4F4E4D4-C4D4E4D4C4C4-"},
        {SongTitle.JingleBells,"E4E4E4-G4C4D4E4-F4F4F4-E4E4E4-E4D4D4E4D4G4-"},
        {SongTitle.FurElise,"E5D#5E5D#5E5B4D4C4A4-C4E4A4B4-E4G#4B4C5-"},
        {SongTitle.CanonInD,"D4F#4A4F#4G4D4G4A4B4F#4G4B4C5#D5-"},
        {SongTitle.Beethoven5th,"G4G4G4Eb4-F4F4F4D4-G4G4G4Eb4-F4F4F4D4-"},
        {SongTitle.Greensleeves,"E4G4B4C5B4G4B4C5D5E4-"},
        {SongTitle.MoonlightSonata, "G#3G#3G#3G#3G#3G#3G#3G#3-A#3A#3A#3A#3A#3A#3A#3A#3-B3F3B3F3#G3F#3G3-B3F3B3F3#G3F#3G3-"}, 
        {SongTitle.ClairDeLune, "D4F4A#4D4F4A#4A#4G4F4G#4A#4B4-"},
        {SongTitle.MinuteWaltz, "E5G5B5D5G5B5E5G5B5E5G5B5D5F#5G5B5E5G5B5E5G5B5D5G5B5E5G5B5D5-"},
        {SongTitle.ToccataAndFugue, "D4A4D5Db4A4C#5D5-D4A4A4A4G4F#4G#4A4-G4F#4G#4A4-B4A4G4F#4G#4A4-"},
        {SongTitle.WilliamTellOverture, "E4D4D4D4E4E4E4E4D4D4D4E4E4E4E4-G4F4G4F4E4D4D4D4E4E4E4E4D4D4D4E4E4E4E4-G4F4G4F4E4-"},
        {SongTitle.SwanLake, "B4G4A4G4A4B4G4B4G4A4G4A4B4G4-"},
        {SongTitle.BachPrelude, "C4C4G4G4C4D4E4E4F4G4A4A4B4C5D5E5F5G5-"},
        {SongTitle.TurkishMarch, "E5B4C5D5E5A4B4C5D5E5B4C5D5E5A4B4C5D5E5D5C5B4A4G#4A4B4C5D5E5B4C5D5E5A4B4C5D5E5B4C5D5E5A4B4C5D5E5D5C5B4A4G#4A4B4C5D5E5C5A4E5C5A4E5C5A4B4D5G4B4D5G4B4D5G4F#4A4E4A4C5E4A4C5E4A4C5F4A4D5F4A4D5F4A4D5G4B4E5G4B4E5G4B4E5F#4A4E4A4C5E4A4C5E4A4C5F4A4D5F4A4D5F4A4D5G4B4E5G4B4E5G4B4E5C5A4E5C5A4E5C5A4B4D5G4B4D5G4B4D5G4F#4A4E4A4C5E4A4C5E4A4C5F4A4D5F4A4D5F4A4D5G4B4E5G4B4E5G4B4E5"}
    };

    private static readonly Dictionary<int, DominoSoundType> dominoSoundTypes = new Dictionary<int, DominoSoundType>
    {
        { 0, DominoSoundType.Click },
        { 1, DominoSoundType.Piano }
    };


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject); // Destroy the previous instance if it exists
        }
        Instance = this;

        InitializeAudioSourcePool();
    }

    void Start()
    {
        // Setup cascade audio
        cascadeSource.clip = cascadeClip;
        cascadeSource.loop = true;
        cascadeSource.volume = 0f;
        cascadeSource.Play();
    }

    void Update()
    {
        // Compute the target volume based on movement and apply global volume scale
        targetVolume = Mathf.Clamp(totalMovement * velocityScale, 0f, maxVolume) * globalVolumeScale;
        // Reset total movement for the next interval
        totalMovement = totalVelocityThreshhold;
        // Smoothly interpolate the actual volume toward the target volume
        cascadeSource.volume = Mathf.Lerp(cascadeSource.volume, targetVolume, Time.deltaTime * volumeLerpSpeed);
    }

    public void SetGlobalVolume(float volume)
    {
        globalVolumeScale = volume; // Set the global volume scale
        cascadeSource.volume = Mathf.Clamp(cascadeSource.volume, 0f, maxVolume) * globalVolumeScale; // Adjust the cascade source volume
    }

    public void SetDominoSound(int value)
    {
        if (dominoSoundTypes.TryGetValue(value, out DominoSoundType soundType))
        {
            userSelectedSoundType = soundType; // Set the user-selected sound type
            Debug.Log($"Domino sound choice set to: {soundType}");
        }
        else
        {
            Debug.LogWarning($"Invalid domino sound choice: {value}");
        }
    }
    

    public void UpdateDominoMovement(float velocityMagnitude)
    {
        if (velocityMagnitude > minimumVelocity)
        {
            totalMovement += Mathf.Clamp(velocityMagnitude, 0f, 20f);
        }
    }

    // Initialize the AudioSource pool
    private void InitializeAudioSourcePool()
    {
        audioSourcePool = new Queue<AudioSource>();

        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            GameObject audioSourceObject = new GameObject($"PooledAudioSource_{i}");
            audioSourceObject.transform.SetParent(transform);
            AudioSource audioSource = audioSourceObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSourcePool.Enqueue(audioSource);
        }
    }

    // Get an available AudioSource from the pool
    private AudioSource GetPooledAudioSource()
    {
        if (audioSourcePool.Count > 0)
        {
            return audioSourcePool.Dequeue();
        }

        // Debug.LogWarning("AudioSource pool exhausted! Consider increasing the pool size.");
        return null;
    }

    // Return an AudioSource to the pool
    private void ReturnAudioSourceToPool(AudioSource audioSource)
    {
        audioSource.Stop();
        audioSource.clip = null;
        audioSourcePool.Enqueue(audioSource);
    }

    public void playArbitrarySound(AudioClip clip, float volume = 1f, float pitch = 1f, Vector3? position = null)
    {
        AudioSource source = GetPooledAudioSource();
        if (source == null) return;

        source.PlayOneShot(clip, volume * globalVolumeScale); // Apply global volume scale
        source.pitch = pitch;
        source.transform.position = position ?? transform.position; // Use provided position or default to transform position

        StartCoroutine(ReturnAudioSourceAfterPlayback(source));
    }

    public void PlayPlacementSound(float pitch = 1f)
    {
        if (Time.time < lastSoundTime + soundCooldown) return;
        lastSoundTime = Time.time;

        AudioSource source = GetPooledAudioSource();
        if (source == null) return;

        source.PlayOneShot(indicatorConfirmSound, 0.5f * globalVolumeScale); // Apply global volume scale
        source.pitch = pitch;
        source.transform.position = transform.position;

        StartCoroutine(ReturnAudioSourceAfterPlayback(source));
    }

    // Play a sound using the pooled AudioSources
    public void PlayDominoSound(float impactForce, Vector3 dominoPosition, DominoSoundType? forcedSoundType = null)
    {
        if (impactForce < minimumImpactForce)
            return;

        AudioSource source = GetPooledAudioSource();
        if (source == null) return;
        if (dominoPosition != null) source.transform.position = dominoPosition;

        // Determine the sound type to play (use forced type if provided, otherwise use user-selected type)
        DominoSoundType soundType = forcedSoundType ?? userSelectedSoundType;

        if (soundType == DominoSoundType.Piano)
        {
            source.pitch = 1; // Set pitch to 1 for music notes
            PlayMusicNote(impactForce, source);
        }
        else if (soundType == DominoSoundType.Click)
        {
            AudioClip clip = dominoClickSounds.sounds[Random.Range(0, dominoClickSounds.sounds.Length)];
            float volume = Mathf.Clamp(impactForce / 40f, 0.1f, 0.5f) * globalVolumeScale; // Apply global volume scale
            source.PlayOneShot(clip, volume);
            source.pitch = 2; // Set pitch to 2 for domino click sounds
        }

        // Return the AudioSource to the pool after the clip finishes playing
        StartCoroutine(ReturnAudioSourceAfterPlayback(source));
    }

    private IEnumerator ReturnAudioSourceAfterPlayback(AudioSource source)
    {
        yield return new WaitWhile(() => source.isPlaying);
        ReturnAudioSourceToPool(source);
    }

    private void PlayMusicNote(float impactForce, AudioSource source)
    {
        //Rate limit the music notes
        if (Time.time < lastSoundTime + soundCooldown)
            return;

        lastSoundTime = Time.time;

        // Get the current song as a note sequence
        string songNotes = songLibrary[currentSong].Replace(" ", ""); // Remove spaces
        string[] notes = ParseNotes(songNotes); // Parse notes into an array
        string note = notes[songIndex % notes.Length]; // Get the current note
        songIndex++; // Move to the next note

        // If the song has completed, switch to the next song
        if (songIndex >= notes.Length)
        {
            songIndex = 0; // Reset the index for the next song
            SwitchToNextSong();
        }

        // If the note is a rest, skip to the next note
        if (note == "-")
        {
            return;
        }

        if (noteMap.TryGetValue(note, out int noteIndex) && noteIndex >= 0 && noteIndex < dominoPianoSounds.sounds.Length)
        {
            noteIndex += octaveOffset * 12; // Adjust for octave offset
            AudioClip clip = dominoPianoSounds.sounds[noteIndex];
            float volume = Mathf.Clamp(impactForce / 20f, 0.1f, 1.0f) * globalVolumeScale; // Apply global volume scale
            source.PlayOneShot(clip, volume);
        }
    }
private string[] ParseNotes(string songNotes)
{
    List<string> parsedNotes = new List<string>();
    int i = 0;

    while (i < songNotes.Length)
    {
        char note = songNotes[i];
        
        // Handle rests
        if (note == '-')
        {
            parsedNotes.Add("-");
            i++;
            continue;
        }

        // Ensure it's a valid note letter (A-G)
        if (note < 'A' || note > 'G')
        {
            i++;
            continue;
        }

        string parsedNote = note.ToString();
        i++;

        // Check for sharp (#) or flat (b)
        if (i < songNotes.Length && (songNotes[i] == '#' || songNotes[i] == 'b'))
        {
            parsedNote += songNotes[i];
            i++;
        }

        // Check for octave number (3-5)
        if (i < songNotes.Length && char.IsDigit(songNotes[i]))
        {
            parsedNote += songNotes[i];
            i++;
        }
        else
        {
            // Default to octave 4 if not specified
            parsedNote += "4";
        }

        parsedNotes.Add(parsedNote);
    }

    return parsedNotes.ToArray();
}


    private void SwitchToNextSong()
    {
        SongTitle[] songKeys = (SongTitle[])System.Enum.GetValues(typeof(SongTitle));
        int currentIndex = System.Array.IndexOf(songKeys, currentSong);
        currentSong = songKeys[(currentIndex + 1) % songKeys.Length];
        Debug.Log("Switched to song: " + currentSong);
    }
}