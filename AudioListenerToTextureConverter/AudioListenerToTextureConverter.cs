using UnityEngine;

/// <summary>
/// Create a texture from a audio listener and apply it in a material
/// </summary>
[ExecuteAlways]
public class AudioListenerToTextureConverter : MonoBehaviour
{
    //==================================================================
    //Declaration
    //==================================================================

    //Int
    [SerializeField] private int resolution = 512;
    //Float
    [SerializeField] float amplitude = 1.5f;
    private float[] spectrum;
    //String
    [SerializeField] private string texturePropertyName;
    //Render
    [SerializeField] private Material materiel;
    private Texture2D texture;

    //==================================================================
    //Methodes automatiques
    //==================================================================

    /// <summary>
    /// Create the texture if the material property isn't null.
    /// </summary>
    private void Start()
    {
        spectrum = new float[resolution];
        texture = new Texture2D(resolution, 1, TextureFormat.RFloat, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        if (materiel != null) materiel.SetTexture(texturePropertyName, texture);
    }

    /// <summary>
    /// Update the texture.
    /// </summary>
    private void Update()
    {
        if (spectrum.Length != resolution) Start();
        AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
        for (int i = 0; i < resolution; i++)
        {
            float value = Mathf.Clamp01(spectrum[i] * amplitude);
            texture.SetPixel(i, 0, new Color(value, 0, 0, 0));
        }
        texture.Apply();
        if (materiel != null) materiel.SetTexture("_TextureSpectre", texture);
    }
}
