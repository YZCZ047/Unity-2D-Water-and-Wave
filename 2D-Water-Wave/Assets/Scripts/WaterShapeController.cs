using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

[ExecuteAlways]
public class WaterShapeController : MonoBehaviour
{
    [Header("整体弹簧参数")]
    [SerializeField][Tooltip("弹簧刚度：越大弹簧弹力越大 液体越硬")] private float springStiffness = 0.1f; // 弹簧刚度
    [SerializeField][Tooltip("阻尼系数：越大停下越快 液体越硬")] private float dampening = 0.05f; // 阻尼系数
    [Tooltip("波浪传播系数：越大对两边点的高度变化影响越大")] public float spread = 0.006f; // 波浪传播系数
    [SerializeField][Tooltip("表面张力：越大曲线越直")] private float tension = 0.33f; //表面张力
    [Range(1,100)][SerializeField][Tooltip("波浪点数量")]private int WavesCount = 6; // 波浪点数量
    [SerializeField][Tooltip("所有弹簧点列表")] private List<WaterSpring> springs = new(); // 所有弹簧点列表

    [Header("波浪参数")]
    [SerializeField][Tooltip("是否开启自流动")] private bool isWaveSelf; // 是否开启自流动
    [SerializeField][Tooltip("波浪材质")] private Material material; //波浪材质
    [SerializeField][Tooltip("波浪移动速度")][Range(0, 10)] private float Speed = 1; //波浪移动速度
    [SerializeField][Tooltip("波浪数量")][Range(0, 20)] private float Count = 0.75f; //波浪数量
    [SerializeField][Tooltip("波浪高度")][Range(0,0.5f)] private float Size = 0.15f; //波浪高度
    [SerializeField][Tooltip("波浪方向")][Range(-1, 1)] private float Direction = 1; //波浪方向
    [SerializeField][Tooltip("是否开启随机浮动")] private bool isRandomSplash; // 是否开启随机浮动
    [SerializeField][Tooltip("随机浮动大小")][Range(0.01f, 0.1f)] private float RandomSplashForce = 0.01f; // 随机浮动大小
    [SerializeField][Tooltip("随机浮动频率")][Range(0.1f, 2)] private float RandomSplashtime = 0.1f; // 随机浮动频率

    [Header("生成设置")]
    [SerializeField][Tooltip("波浪点预制体")] private GameObject wavePointPref; // 波浪点预制体
    [SerializeField][Tooltip("波浪点父物体")] private GameObject wavePoints; // 波浪点父物体
    [SerializeField][Tooltip("水体控制器")] private SpriteShapeController spriteShapeController; // 水体控制器

    private int CorsnersCount = 2; // 边界固定点数（左右各1）
    private float spacingPerWave; //波浪点间距
    private bool flag;


    #region 初始化
    // 编辑器模式下的更新
    private void OnValidate()
    {
        StartCoroutine(CreateWave()); // 参数变化时重建波浪
    }

    // 重建波浪协程
    IEnumerator CreateWave()
    {
        // 删除旧波浪点
        foreach (Transform chlid in wavePoints.transform)
        {
            StartCoroutine(Destroy(chlid.gameObject));
        }
        yield return null; // 等待一帧
        SetWaves(); // 重建波浪
        yield return null;
    }

    // 安全销毁对象协程
    IEnumerator Destroy(GameObject go)
    {
        yield return null;
        DestroyImmediate(go);
    }

    // 设置波浪点（重建Spline）
    private void SetWaves()
    {
        if (spriteShapeController == null) //获取组件
            spriteShapeController = GetComponent<SpriteShapeController>();

        Spline waterSpline = spriteShapeController.spline; //获取组件

        int waterPointsCount = waterSpline.GetPointCount(); //获取点总数

        // 移除旧波浪点（保留左右边界）
        for (int i = CorsnersCount; i < waterPointsCount - CorsnersCount; i++)
        {
            waterSpline.RemovePointAt(CorsnersCount); //由2开始,下边界不改变
        }

        // 获取左右边界位置
        Vector3 waterTopLeftCorner = waterSpline.GetPosition(1);
        Vector3 waterTopRightCorner = waterSpline.GetPosition(2);
        float waterWidth = waterTopRightCorner.x - waterTopLeftCorner.x; //计算总宽度

        // 计算波浪点间距
        spacingPerWave = waterWidth / (WavesCount + 1); //（波浪点+端点-1=线段数）

        // 插入新波浪点
        for (int i = WavesCount; i > 0; i--)
        {
            int index = CorsnersCount; // 插入位置（左边界后）

            // 计算点X位置（均匀分布） 以左边界起始
            float xPosition = waterTopLeftCorner.x + (spacingPerWave * i);
            //只改变x位置
            Vector3 wavePoint = new Vector3(xPosition, waterTopLeftCorner.y, waterTopLeftCorner.z);
            
            // 插入并配置Spline点
            waterSpline.InsertPointAt(index, wavePoint); //设置位置
            waterSpline.SetCorner(index, false); // 设为曲线点（非角点）
            waterSpline.SetTangentMode(index, ShapeTangentMode.Continuous); // 连续切线模式
        }

        for (int i = waterSpline.GetPointCount()-1; i >= 0; i--)
        {
            waterSpline.SetHeight(i, 0.01f); // 设置所有点切线手柄长度
        }

        CreateSprings(waterSpline); // 创建弹簧组件

        if (isWaveSelf)
            SetWaveMove(); //应用波浪参数

        if (isRandomSplash)
        {
            CancelInvoke(nameof(RandomSplash)); //值改变时，取消随机浮动
            InvokeRepeating(nameof(RandomSplash),0, RandomSplashtime); //启动随机浮动
        }
    }

    // 创建弹簧组件
    private void CreateSprings(Spline waterSpline)
    {
        springs = new(); // 重置列表

        for (int i = 1; i <= WavesCount + 2; i++)
        {

            Smoothen(waterSpline,i); // 平滑该点

            GameObject wavePoint = Instantiate(wavePointPref, wavePoints.transform, false); // 实例化波浪点
            wavePoint.transform.localPosition = waterSpline.GetPosition(i); //设置位置到对应点

            // 初始化弹簧组件
            WaterSpring waterSpring = wavePoint.GetComponent<WaterSpring>();
            waterSpring.Init(spriteShapeController);
            springs.Add(waterSpring);
        }
    }

    // 平滑Spline点（调整切线）
    private void Smoothen(Spline waterSpline, int index)
    {
        Vector3 position = waterSpline.GetPosition(index); //获取点位置
        Vector3 positionPrev = position; //初始化前一个点位置
        Vector3 positionNexv = position; //初始化后一个点位置

        // 获取相邻点位置
        if (index > 1) //若未过左边界
            positionPrev = waterSpline.GetPosition(index - 1);
        if (index - 1 <= WavesCount) //若未过右边界
            positionNexv = waterSpline.GetPosition(index + 1);

        Vector3 forward = gameObject.transform.forward;

        // 计算切线缩放因子
        float scale = Mathf.Min(
            (positionNexv - position).magnitude,
            (positionPrev - position).magnitude
            ) * tension;// 使用张力计算切线长度

        // 计算切线方向
        Vector3 leftTangent = (positionPrev - position).normalized * scale;
        Vector3 rightTangent = (positionNexv - position).normalized * scale;

        // 使用Unity工具计算更平滑的切线
        SplineUtility.CalculateTangents(position, positionPrev, positionNexv,
            forward, scale, out rightTangent, out leftTangent);

        // 应用切线
        waterSpline.SetLeftTangent(index, leftTangent);
        waterSpline.SetRightTangent(index, rightTangent);
    }

    private void SetWaveMove() //应用波浪参数
    {
        SpriteShapeRenderer spriteShapeRenderer = GetComponent<SpriteShapeRenderer>();

        // 创建材质实例
        if (spriteShapeRenderer != null && material != null)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock(); //通过属性块传递数据，减少内存占用
            spriteShapeRenderer.GetPropertyBlock(propertyBlock, 1); //获取属性块

            propertyBlock.SetFloat("_Speed",Speed); //设置参数
            propertyBlock.SetFloat("_Count",Count);
            propertyBlock.SetFloat("_Size",Size);
            propertyBlock.SetFloat("_Direction",Direction);

            spriteShapeRenderer.SetPropertyBlock(propertyBlock, 1); //传递属性块
        }
    }

    #endregion

    #region 物理更新
    // 物理更新
    private void FixedUpdate()
    {
        // 更新所有弹簧物理
        for (int i = 0; i < springs.Count; i++)
        {
            // 更新弹簧位置
            springs[i].WaveSpringUpdate(springStiffness,dampening);
            springs[i].WavePointUpdate(); // 更新Spline位置

        }

        UPdateSpings(); // 处理波浪传播
    }

    // 波浪传播计算
    private void UPdateSpings()
    {

        int count = springs.Count;

        float[] forceDeltasY = new float[count]; // Y轴传播力
        float[] forceDeltasX = new float[count]; // X轴传播力

        // 第一遍：计算传播力
        for (int i = 0; i < count; i++)
        {
            // 向左传播
            if (i > 0)
            {
                //传播力 = 传播系数 乘 高度差/位移差
                float deltaY = spread * (springs[i].Yheight - springs[i - 1].Yheight);
                forceDeltasY[i - 1] += deltaY;
                float deltaX = spread * (spacingPerWave - (springs[i].Xheight - springs[i - 1].Xheight)); // 减去间隔，防止无限拉伸
                forceDeltasX[i - 1] += deltaX;
            }
            // 向右传播
            if (i < springs.Count - 1)
            {
                float deltaY = spread * (springs[i].Yheight - springs[i + 1].Yheight);
                forceDeltasY[i + 1] += deltaY;
                float deltaX = spread * (spacingPerWave + (springs[i].Xheight - springs[i + 1].Xheight));
                forceDeltasX[i + 1] += deltaX;
            }
        }

        // 第二遍：统一应用速度变化
        for (int i = 0; i < count; i++)
        {
            springs[i].Yvelocity += forceDeltasY[i];
            springs[i].Xvelocity += forceDeltasX[i];
        }
        
    }

    #endregion
    private void RandomSplash() //随机浮动函数
    {
        Splash(UnityEngine.Random.Range(1, WavesCount), RandomSplashForce);
    }

    // 手动在指定位置制造波浪
    private void Splash(int i, float speed)
    {
        if (i >= 0 && i < springs.Count)
        {
            springs[i].Yvelocity += speed;
        }
    }

}
