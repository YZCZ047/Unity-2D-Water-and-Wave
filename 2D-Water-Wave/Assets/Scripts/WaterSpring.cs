using UnityEngine;
using UnityEngine.U2D;

[ExecuteAlways]
public class WaterSpring : MonoBehaviour
{
    [Header("单个弹簧参数")]
    [SerializeField][Tooltip("Y轴降低摆动幅度的参数")] private int VlimitY = 50; //Y轴降低摆动幅度的参数
    [SerializeField][Tooltip("X轴降低摆动幅度的参数")] private int VlimitX = 50; //X轴降低摆动幅度的参数
    [SerializeField][Tooltip("限制最大摆动幅度的参数")] private int Vlimit = 5; //限制最大摆动幅度的参数
    [SerializeField][Tooltip("当前点在Spline中的索引位置")] private int waveIndex; // 当前点在Spline中的索引位置
    private SpriteShapeController spriteShapeController;// 关联的水体形状控制器

    [Header("弹簧相关变量")]
    [Tooltip("当前点的垂直速度")] public float Yvelocity = 0; //当前点的垂直速度
    [Tooltip("当前点垂直的受力")] public float Yforce = 0; //当前点垂直的受力
    [Tooltip("当前点的实际高度")] public float Yheight = 0; // 当前点的实际高度
    [SerializeField][Tooltip("水面静止时的高度")] private float Ytarget_height = 0; // 目标高度（水面静止时的高度）
    [Tooltip("当前点的水平速度")] public float Xvelocity = 0; //当前点的水平速度
    [Tooltip("当前点水平的受力")] public float Xforce = 0; //当前点水平的受力
    [Tooltip("当前点的实际水平位置")] public float Xheight = 0; // 当前点的实际水平位置
    [SerializeField][Tooltip("水面静止时的水平位置")] private float Xtarget_height = 0; // 目标水平位置（水面静止时的水平位置）

    #region 初始化
    // 初始化弹簧
    public void Init(SpriteShapeController ssc)
    {
        var i = transform.GetSiblingIndex(); // 获取在父物体中的序号
        waveIndex = i + 1; // 计算在Spline中的索引（+1跳过左边界点）
        spriteShapeController = ssc;

        Yvelocity = 0; // 初始化物理状态
        Yheight = transform.localPosition.y;
        Ytarget_height = Yheight; // 初始目标高度=当前位置

        Xvelocity = 0;
        Xheight = transform.localPosition.x;
        Xtarget_height = Xheight; // 初始目标水平位置=当前水平位置
    }
    #endregion

    #region 物理更新
    // 更新弹簧物理状态
    public void WaveSpringUpdate(float springStiffness,float dampening)
    {
        Yheight = transform.localPosition.y; // 获取当前Y轴位置作为高度
        Xheight = transform.localPosition.x; // 获取当前X轴位置作为水平位置

        var y1 = Ytarget_height - Yheight; // 计算与目标高度的偏差
        var x1 = Xtarget_height - Xheight; // 计算与目标水平位置的偏差
        var lossY = -dampening * Yvelocity; // 计算Y轴阻尼力（与速度方向相反）
        var lossX = -dampening * Xvelocity; // 计算X轴阻尼力

        Yforce = springStiffness * y1 + lossY; // 计算Y轴合力：胡克定律 + 阻尼力
        Xforce = springStiffness * x1 + lossX; // 计算X轴合力
        Yvelocity += Yforce; // 更新Y轴速度（省略了质量项，相当于质量=1）
        Xvelocity += Xforce; // 更新X轴速度

        // 应用速度改变位置
        Vector3 pos = transform.localPosition;
        pos.y += Yvelocity;
        pos.x += Xvelocity;
        transform.localPosition = pos;
    }

    // 将弹簧位置更新到水体Spline的点
    public void WavePointUpdate()
    {
        if (spriteShapeController != null)
        {
            Spline waterSpline = spriteShapeController.spline;
            if (waveIndex < waterSpline.GetPointCount()) //防止当重新生成弹簧点的时候空索引
            {

                Vector3 wavePosition = waterSpline.GetPosition(waveIndex);
                // 只更新Y坐标，保持XZ不变
                waterSpline.SetPosition(waveIndex, new Vector3(
                    transform.localPosition.x,
                    transform.localPosition.y,
                    wavePosition.z));
            }
        }
    }
    #endregion

    #region 物体入水碰撞检测
    // 碰撞检测（物体入水时）
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //if (collision.gameObject.CompareTag("Words")) 自行更改tag
        //{
            // 使用TryGetComponent避免无效的GetComponent调用
            if (collision.rigidbody.TryGetComponent<Rigidbody2D>(out var rb))
            {
                var speed = rb.velocity;

                // 根据物体速度添加扰动（限制幅度）
                Yvelocity += Mathf.Clamp(speed.y / VlimitY, -Vlimit, Vlimit);
                Xvelocity += Mathf.Clamp(speed.x / VlimitX, -Vlimit, Vlimit);
            }
        //}
    }
    #endregion
}
