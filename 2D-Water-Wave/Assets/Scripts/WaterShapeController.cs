using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

[ExecuteAlways]
public class WaterShapeController : MonoBehaviour
{
    [Header("���嵯�ɲ���")]
    [SerializeField][Tooltip("���ɸնȣ�Խ�󵯻ɵ���Խ�� Һ��ԽӲ")] private float springStiffness = 0.1f; // ���ɸն�
    [SerializeField][Tooltip("����ϵ����Խ��ͣ��Խ�� Һ��ԽӲ")] private float dampening = 0.05f; // ����ϵ��
    [Tooltip("���˴���ϵ����Խ������ߵ�ĸ߶ȱ仯Ӱ��Խ��")] public float spread = 0.006f; // ���˴���ϵ��
    [SerializeField][Tooltip("����������Խ������Խֱ")] private float tension = 0.33f; //��������
    [Range(1,100)][SerializeField][Tooltip("���˵�����")]private int WavesCount = 6; // ���˵�����
    [SerializeField][Tooltip("���е��ɵ��б�")] private List<WaterSpring> springs = new(); // ���е��ɵ��б�

    [Header("���˲���")]
    [SerializeField][Tooltip("�Ƿ���������")] private bool isWaveSelf; // �Ƿ���������
    [SerializeField][Tooltip("���˲���")] private Material material; //���˲���
    [SerializeField][Tooltip("�����ƶ��ٶ�")][Range(0, 10)] private float Speed = 1; //�����ƶ��ٶ�
    [SerializeField][Tooltip("��������")][Range(0, 20)] private float Count = 0.75f; //��������
    [SerializeField][Tooltip("���˸߶�")][Range(0,0.5f)] private float Size = 0.15f; //���˸߶�
    [SerializeField][Tooltip("���˷���")][Range(-1, 1)] private float Direction = 1; //���˷���
    [SerializeField][Tooltip("�Ƿ����������")] private bool isRandomSplash; // �Ƿ����������
    [SerializeField][Tooltip("���������С")][Range(0.01f, 0.1f)] private float RandomSplashForce = 0.01f; // ���������С
    [SerializeField][Tooltip("�������Ƶ��")][Range(0.1f, 2)] private float RandomSplashtime = 0.1f; // �������Ƶ��

    [Header("��������")]
    [SerializeField][Tooltip("���˵�Ԥ����")] private GameObject wavePointPref; // ���˵�Ԥ����
    [SerializeField][Tooltip("���˵㸸����")] private GameObject wavePoints; // ���˵㸸����
    [SerializeField][Tooltip("ˮ�������")] private SpriteShapeController spriteShapeController; // ˮ�������

    private int CorsnersCount = 2; // �߽�̶����������Ҹ�1��
    private float spacingPerWave; //���˵���
    private bool flag;


    #region ��ʼ��
    // �༭��ģʽ�µĸ���
    private void OnValidate()
    {
        StartCoroutine(CreateWave()); // �����仯ʱ�ؽ�����
    }

    // �ؽ�����Э��
    IEnumerator CreateWave()
    {
        // ɾ���ɲ��˵�
        foreach (Transform chlid in wavePoints.transform)
        {
            StartCoroutine(Destroy(chlid.gameObject));
        }
        yield return null; // �ȴ�һ֡
        SetWaves(); // �ؽ�����
        yield return null;
    }

    // ��ȫ���ٶ���Э��
    IEnumerator Destroy(GameObject go)
    {
        yield return null;
        DestroyImmediate(go);
    }

    // ���ò��˵㣨�ؽ�Spline��
    private void SetWaves()
    {
        if (spriteShapeController == null) //��ȡ���
            spriteShapeController = GetComponent<SpriteShapeController>();

        Spline waterSpline = spriteShapeController.spline; //��ȡ���

        int waterPointsCount = waterSpline.GetPointCount(); //��ȡ������

        // �Ƴ��ɲ��˵㣨�������ұ߽磩
        for (int i = CorsnersCount; i < waterPointsCount - CorsnersCount; i++)
        {
            waterSpline.RemovePointAt(CorsnersCount); //��2��ʼ,�±߽粻�ı�
        }

        // ��ȡ���ұ߽�λ��
        Vector3 waterTopLeftCorner = waterSpline.GetPosition(1);
        Vector3 waterTopRightCorner = waterSpline.GetPosition(2);
        float waterWidth = waterTopRightCorner.x - waterTopLeftCorner.x; //�����ܿ��

        // ���㲨�˵���
        spacingPerWave = waterWidth / (WavesCount + 1); //�����˵�+�˵�-1=�߶�����

        // �����²��˵�
        for (int i = WavesCount; i > 0; i--)
        {
            int index = CorsnersCount; // ����λ�ã���߽��

            // �����Xλ�ã����ȷֲ��� ����߽���ʼ
            float xPosition = waterTopLeftCorner.x + (spacingPerWave * i);
            //ֻ�ı�xλ��
            Vector3 wavePoint = new Vector3(xPosition, waterTopLeftCorner.y, waterTopLeftCorner.z);
            
            // ���벢����Spline��
            waterSpline.InsertPointAt(index, wavePoint); //����λ��
            waterSpline.SetCorner(index, false); // ��Ϊ���ߵ㣨�ǽǵ㣩
            waterSpline.SetTangentMode(index, ShapeTangentMode.Continuous); // ��������ģʽ
        }

        for (int i = waterSpline.GetPointCount()-1; i >= 0; i--)
        {
            waterSpline.SetHeight(i, 0.01f); // �������е������ֱ�����
        }

        CreateSprings(waterSpline); // �����������

        if (isWaveSelf)
            SetWaveMove(); //Ӧ�ò��˲���

        if (isRandomSplash)
        {
            CancelInvoke(nameof(RandomSplash)); //ֵ�ı�ʱ��ȡ���������
            InvokeRepeating(nameof(RandomSplash),0, RandomSplashtime); //�����������
        }
    }

    // �����������
    private void CreateSprings(Spline waterSpline)
    {
        springs = new(); // �����б�

        for (int i = 1; i <= WavesCount + 2; i++)
        {

            Smoothen(waterSpline,i); // ƽ���õ�

            GameObject wavePoint = Instantiate(wavePointPref, wavePoints.transform, false); // ʵ�������˵�
            wavePoint.transform.localPosition = waterSpline.GetPosition(i); //����λ�õ���Ӧ��

            // ��ʼ���������
            WaterSpring waterSpring = wavePoint.GetComponent<WaterSpring>();
            waterSpring.Init(spriteShapeController);
            springs.Add(waterSpring);
        }
    }

    // ƽ��Spline�㣨�������ߣ�
    private void Smoothen(Spline waterSpline, int index)
    {
        Vector3 position = waterSpline.GetPosition(index); //��ȡ��λ��
        Vector3 positionPrev = position; //��ʼ��ǰһ����λ��
        Vector3 positionNexv = position; //��ʼ����һ����λ��

        // ��ȡ���ڵ�λ��
        if (index > 1) //��δ����߽�
            positionPrev = waterSpline.GetPosition(index - 1);
        if (index - 1 <= WavesCount) //��δ���ұ߽�
            positionNexv = waterSpline.GetPosition(index + 1);

        Vector3 forward = gameObject.transform.forward;

        // ����������������
        float scale = Mathf.Min(
            (positionNexv - position).magnitude,
            (positionPrev - position).magnitude
            ) * tension;// ʹ�������������߳���

        // �������߷���
        Vector3 leftTangent = (positionPrev - position).normalized * scale;
        Vector3 rightTangent = (positionNexv - position).normalized * scale;

        // ʹ��Unity���߼����ƽ��������
        SplineUtility.CalculateTangents(position, positionPrev, positionNexv,
            forward, scale, out rightTangent, out leftTangent);

        // Ӧ������
        waterSpline.SetLeftTangent(index, leftTangent);
        waterSpline.SetRightTangent(index, rightTangent);
    }

    private void SetWaveMove() //Ӧ�ò��˲���
    {
        SpriteShapeRenderer spriteShapeRenderer = GetComponent<SpriteShapeRenderer>();

        // ��������ʵ��
        if (spriteShapeRenderer != null && material != null)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock(); //ͨ�����Կ鴫�����ݣ������ڴ�ռ��
            spriteShapeRenderer.GetPropertyBlock(propertyBlock, 1); //��ȡ���Կ�

            propertyBlock.SetFloat("_Speed",Speed); //���ò���
            propertyBlock.SetFloat("_Count",Count);
            propertyBlock.SetFloat("_Size",Size);
            propertyBlock.SetFloat("_Direction",Direction);

            spriteShapeRenderer.SetPropertyBlock(propertyBlock, 1); //�������Կ�
        }
    }

    #endregion

    #region �������
    // �������
    private void FixedUpdate()
    {
        // �������е�������
        for (int i = 0; i < springs.Count; i++)
        {
            // ���µ���λ��
            springs[i].WaveSpringUpdate(springStiffness,dampening);
            springs[i].WavePointUpdate(); // ����Splineλ��

        }

        UPdateSpings(); // �����˴���
    }

    // ���˴�������
    private void UPdateSpings()
    {

        int count = springs.Count;

        float[] forceDeltasY = new float[count]; // Y�ᴫ����
        float[] forceDeltasX = new float[count]; // X�ᴫ����

        // ��һ�飺���㴫����
        for (int i = 0; i < count; i++)
        {
            // ���󴫲�
            if (i > 0)
            {
                //������ = ����ϵ�� �� �߶Ȳ�/λ�Ʋ�
                float deltaY = spread * (springs[i].Yheight - springs[i - 1].Yheight);
                forceDeltasY[i - 1] += deltaY;
                float deltaX = spread * (spacingPerWave - (springs[i].Xheight - springs[i - 1].Xheight)); // ��ȥ�������ֹ��������
                forceDeltasX[i - 1] += deltaX;
            }
            // ���Ҵ���
            if (i < springs.Count - 1)
            {
                float deltaY = spread * (springs[i].Yheight - springs[i + 1].Yheight);
                forceDeltasY[i + 1] += deltaY;
                float deltaX = spread * (spacingPerWave + (springs[i].Xheight - springs[i + 1].Xheight));
                forceDeltasX[i + 1] += deltaX;
            }
        }

        // �ڶ��飺ͳһӦ���ٶȱ仯
        for (int i = 0; i < count; i++)
        {
            springs[i].Yvelocity += forceDeltasY[i];
            springs[i].Xvelocity += forceDeltasX[i];
        }
        
    }

    #endregion
    private void RandomSplash() //�����������
    {
        Splash(UnityEngine.Random.Range(1, WavesCount), RandomSplashForce);
    }

    // �ֶ���ָ��λ�����첨��
    private void Splash(int i, float speed)
    {
        if (i >= 0 && i < springs.Count)
        {
            springs[i].Yvelocity += speed;
        }
    }

}
