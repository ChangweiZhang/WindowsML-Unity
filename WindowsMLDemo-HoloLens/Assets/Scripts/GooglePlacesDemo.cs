using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GooglePlacesDemo : MonoBehaviour
{
    bool isReadyToPreview = false;
    public TextMesh m_ResultText;
    public TextMesh m_EvalutionTimeText;
    public Material m_PreviewMaterial;
    private AIDemoService demoService = AIDemoService.Instance;
    // Use this for initialization
    async void Start()
    {
        await demoService.LoadModelAsync();
        await demoService.StartPreviewAsync();
#if WINDOWS_UWP
#endif
    }

    // Update is called once per frame
    async void Update()
    {
        if (demoService.isPreviewing && !isReadyToPreview)
        {
            isReadyToPreview = true;
            await demoService.StartDetectAsync(6, 224, 224, true);
        }
        if (isReadyToPreview)
        {
            m_ResultText.text = demoService.DetectResult;
            m_EvalutionTimeText.text = demoService.EvalutionTime;
            if (demoService.PreviewData != null && demoService.PreviewData.Length > 0)
            {
                var texture = new Texture2D(224, 224);
                texture.LoadRawTextureData(demoService.PreviewData);
                m_PreviewMaterial.mainTexture = texture;
            }
        }
    }
}
