using UnityEngine;
using UnityEngine.U2D;

[ExecuteAlways]
public class WaterSpring : MonoBehaviour
{
    [Header("�������ɲ���")]
    [SerializeField][Tooltip("Y�ή�Ͱڶ����ȵĲ���")] private int VlimitY = 50; //Y�ή�Ͱڶ����ȵĲ���
    [SerializeField][Tooltip("X�ή�Ͱڶ����ȵĲ���")] private int VlimitX = 50; //X�ή�Ͱڶ����ȵĲ���
    [SerializeField][Tooltip("�������ڶ����ȵĲ���")] private int Vlimit = 5; //�������ڶ����ȵĲ���
    [SerializeField][Tooltip("��ǰ����Spline�е�����λ��")] private int waveIndex; // ��ǰ����Spline�е�����λ��
    private SpriteShapeController spriteShapeController;// ������ˮ����״������

    [Header("������ر���")]
    [Tooltip("��ǰ��Ĵ�ֱ�ٶ�")] public float Yvelocity = 0; //��ǰ��Ĵ�ֱ�ٶ�
    [Tooltip("��ǰ�㴹ֱ������")] public float Yforce = 0; //��ǰ�㴹ֱ������
    [Tooltip("��ǰ���ʵ�ʸ߶�")] public float Yheight = 0; // ��ǰ���ʵ�ʸ߶�
    [SerializeField][Tooltip("ˮ�澲ֹʱ�ĸ߶�")] private float Ytarget_height = 0; // Ŀ��߶ȣ�ˮ�澲ֹʱ�ĸ߶ȣ�
    [Tooltip("��ǰ���ˮƽ�ٶ�")] public float Xvelocity = 0; //��ǰ���ˮƽ�ٶ�
    [Tooltip("��ǰ��ˮƽ������")] public float Xforce = 0; //��ǰ��ˮƽ������
    [Tooltip("��ǰ���ʵ��ˮƽλ��")] public float Xheight = 0; // ��ǰ���ʵ��ˮƽλ��
    [SerializeField][Tooltip("ˮ�澲ֹʱ��ˮƽλ��")] private float Xtarget_height = 0; // Ŀ��ˮƽλ�ã�ˮ�澲ֹʱ��ˮƽλ�ã�

    #region ��ʼ��
    // ��ʼ������
    public void Init(SpriteShapeController ssc)
    {
        var i = transform.GetSiblingIndex(); // ��ȡ�ڸ������е����
        waveIndex = i + 1; // ������Spline�е�������+1������߽�㣩
        spriteShapeController = ssc;

        Yvelocity = 0; // ��ʼ������״̬
        Yheight = transform.localPosition.y;
        Ytarget_height = Yheight; // ��ʼĿ��߶�=��ǰλ��

        Xvelocity = 0;
        Xheight = transform.localPosition.x;
        Xtarget_height = Xheight; // ��ʼĿ��ˮƽλ��=��ǰˮƽλ��
    }
    #endregion

    #region �������
    // ���µ�������״̬
    public void WaveSpringUpdate(float springStiffness,float dampening)
    {
        Yheight = transform.localPosition.y; // ��ȡ��ǰY��λ����Ϊ�߶�
        Xheight = transform.localPosition.x; // ��ȡ��ǰX��λ����Ϊˮƽλ��

        var y1 = Ytarget_height - Yheight; // ������Ŀ��߶ȵ�ƫ��
        var x1 = Xtarget_height - Xheight; // ������Ŀ��ˮƽλ�õ�ƫ��
        var lossY = -dampening * Yvelocity; // ����Y�������������ٶȷ����෴��
        var lossX = -dampening * Xvelocity; // ����X��������

        Yforce = springStiffness * y1 + lossY; // ����Y����������˶��� + ������
        Xforce = springStiffness * x1 + lossX; // ����X�����
        Yvelocity += Yforce; // ����Y���ٶȣ�ʡ����������൱������=1��
        Xvelocity += Xforce; // ����X���ٶ�

        // Ӧ���ٶȸı�λ��
        Vector3 pos = transform.localPosition;
        pos.y += Yvelocity;
        pos.x += Xvelocity;
        transform.localPosition = pos;
    }

    // ������λ�ø��µ�ˮ��Spline�ĵ�
    public void WavePointUpdate()
    {
        if (spriteShapeController != null)
        {
            Spline waterSpline = spriteShapeController.spline;
            if (waveIndex < waterSpline.GetPointCount()) //��ֹ���������ɵ��ɵ��ʱ�������
            {

                Vector3 wavePosition = waterSpline.GetPosition(waveIndex);
                // ֻ����Y���꣬����XZ����
                waterSpline.SetPosition(waveIndex, new Vector3(
                    transform.localPosition.x,
                    transform.localPosition.y,
                    wavePosition.z));
            }
        }
    }
    #endregion

    #region ������ˮ��ײ���
    // ��ײ��⣨������ˮʱ��
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //if (collision.gameObject.CompareTag("Words")) ���и���tag
        //{
            // ʹ��TryGetComponent������Ч��GetComponent����
            if (collision.rigidbody.TryGetComponent<Rigidbody2D>(out var rb))
            {
                var speed = rb.velocity;

                // ���������ٶ�����Ŷ������Ʒ��ȣ�
                Yvelocity += Mathf.Clamp(speed.y / VlimitY, -Vlimit, Vlimit);
                Xvelocity += Mathf.Clamp(speed.x / VlimitX, -Vlimit, Vlimit);
            }
        //}
    }
    #endregion
}
