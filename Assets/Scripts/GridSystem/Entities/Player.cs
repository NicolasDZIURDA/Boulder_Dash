using UnityEngine;

public class Player : MonoBehaviour
{
    public Vector2Int MoveInput { get; private set; }
    public bool isDead { get; private set; }

    private Vector2Int heldInput;
    private float inputRepeatDelay = 0.1f;
    private float inputTimer;

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        Vector2Int newInput = Vector2Int.zero;

        if (Input.GetKey(KeyCode.RightArrow))
            newInput = Vector2Int.right;
        else if (Input.GetKey(KeyCode.LeftArrow))
            newInput = Vector2Int.left;
        else if (Input.GetKey(KeyCode.UpArrow))
            newInput = Vector2Int.up;
        else if (Input.GetKey(KeyCode.DownArrow))
            newInput = Vector2Int.down;

        if (newInput != Vector2Int.zero)
        {
            if (newInput != heldInput)
            {
                MoveInput = newInput;
                heldInput = newInput;
                inputTimer = 0f;
            }
            else
            {
                // maintien → répétition contrôlée
                inputTimer += Time.deltaTime;

                if (inputTimer >= inputRepeatDelay)
                {
                    MoveInput = newInput;
                    inputTimer = 0f;
                }
            }
        }
        else
        {
            heldInput = Vector2Int.zero;
            inputTimer = 0f;
        }
    }

    public void ConsumeInput()
    {
        MoveInput = Vector2Int.zero;
    }

    public void KillPlayer()
    {
        isDead = true;
        gameObject.SetActive(false);
    }
}