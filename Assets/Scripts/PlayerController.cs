using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -20f;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private CharacterController characterController;
    private Vector3 velocity;
    private bool facingRight = true;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Update()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        if (horizontalInput != 0f)
        {
            SetFacing(horizontalInput > 0f);
        }

        Vector3 move = new Vector3(horizontalInput, 0f, 0f) * moveSpeed;
        characterController.Move(move * Time.deltaTime);

        if (characterController.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        if (Input.GetButtonDown("Jump") && characterController.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void SetFacing(bool shouldFaceRight)
    {
        if (facingRight == shouldFaceRight)
        {
            return;
        }

        facingRight = shouldFaceRight;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !facingRight;
            return;
        }

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (facingRight ? 1f : -1f);
        transform.localScale = scale;
    }
}
