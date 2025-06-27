using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpForce = 7f;
    public float gravity = -20f; // Ajusta según sea necesario

    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    public Transform cameraHolder; // Asigna un GameObject hijo para sostener la cámara
    public float maxLookUp = -50f; // Ángulo máximo para mirar hacia arriba (negativo)
    public float minLookDown = 80f;  // Ángulo máximo para mirar hacia abajo

    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool isGrounded;
    private float xRotation = 0f; // Rotación vertical de la cámara

    // Referencia a la cámara principal (se asignará si es el jugador local)
    private Camera mainCamera;

    [Header("Shooting Settings")]
    public float fireRate = 0.25f; // Tiempo entre disparos (0.25f = 4 disparos por segundo)
    public float weaponRange = 50f; // Alcance del arma
    public float hitForce = 100f; // Fuerza aplicada al objeto impactado (si tiene Rigidbody)
    public int weaponDamage = 20; // Daño del arma
    // public Transform gunEnd; // Opcional: Un punto en el arma desde donde sale el rayo/proyectil

    private float nextFireTime = 0f; // Para controlar la cadencia de fuego
    // private LineRenderer laserLine; // Opcional: para visualizar el disparo

    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;


    void Awake() // Cambiado de Start a Awake para asegurar que laserLine se configura antes si se usa
    {
        controller = GetComponent<CharacterController>();
        // Si decides usar un LineRenderer para visualizar el disparo:
        // laserLine = GetComponent<LineRenderer>();
    }

    void Start()
    {
        if (controller == null)
        {
            Debug.LogError("PlayerController: CharacterController component not found on player prefab!");
            enabled = false;
            return;
        }

        if (photonView.IsMine)
        {
            // Asignar y configurar la cámara principal solo para el jugador local
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                if (cameraHolder != null)
                {
                    mainCamera.transform.SetParent(cameraHolder);
                    mainCamera.transform.localPosition = Vector3.zero;
                    mainCamera.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    Debug.LogError("PlayerController: CameraHolder transform is not assigned in the Inspector.");
                }
            }
            else
            {
                Debug.LogError("PlayerController: Main Camera not found. Make sure you have a camera tagged 'MainCamera' in your scene.");
            }

            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.blue;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            currentHealth = maxHealth; // Salud inicial para el jugador local
        }
        else
        {
            if (controller != null)
            {
                controller.enabled = false;
            }

            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.red;
            }
            currentHealth = maxHealth; // Todos parten con salud máxima visiblemente.
        }
    }

    void Update()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        isGrounded = controller.isGrounded;

        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        bool jumpPressed = Input.GetButtonDown("Jump");

        Vector3 moveDirection = transform.right * horizontalInput + transform.forward * verticalInput;
        moveDirection.Normalize();

        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        if (jumpPressed && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        if (cameraHolder != null)
        {
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, maxLookUp, minLookDown);
            cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        // --- Shooting Input ---
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime) // "Fire1" es el clic izquierdo por defecto
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        Debug.Log(photonView.Owner.NickName + " is shooting!");
        Vector3 rayOrigin = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0.0f));
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, mainCamera.transform.forward, out hit, weaponRange))
        {
            Debug.Log("Hit: " + hit.collider.name);
            PhotonView targetPhotonView = hit.collider.GetComponent<PhotonView>();
            if (targetPhotonView != null)
            {
                photonView.RPC("ApplyHit", RpcTarget.All, targetPhotonView.ViewID, weaponDamage, hit.point, hit.normal);
            }
            else if (hit.rigidbody != null)
            {
                hit.rigidbody.AddForce(-hit.normal * hitForce);
            }
            // if (laserLine != null)
            // {
            //     StartCoroutine(ShotEffect(hit.point));
            // }
        }
        else
        {
            // if (laserLine != null)
            // {
            //     StartCoroutine(ShotEffect(rayOrigin + mainCamera.transform.forward * weaponRange));
            // }
        }
    }

    // IEnumerator ShotEffect(Vector3 hitPosition)
    // {
    //     laserLine.SetPosition(0, gunEnd != null ? gunEnd.position : mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0.0f)));
    //     laserLine.SetPosition(1, hitPosition);
    //     laserLine.enabled = true;
    //     yield return new WaitForSeconds(0.05f);
    //     laserLine.enabled = false;
    // }

    [PunRPC]
    void ApplyHit(int targetViewID, int damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        Debug.Log($"RPC ApplyHit received for targetViewID: {targetViewID} with damage: {damage}");
        PhotonView targetPV = PhotonView.Find(targetViewID);
        if (targetPV != null)
        {
            Debug.Log($"Target PhotonView found: {targetPV.gameObject.name}");
            PlayerController hitPlayerController = targetPV.GetComponent<PlayerController>();
            if (hitPlayerController != null)
            {
                hitPlayerController.TakeDamage(damage, photonView.Owner);
            }
        }
        else
        {
            Debug.LogWarning($"RPC ApplyHit: Target PhotonView with ID {targetViewID} not found.");
        }
    }

    public void TakeDamage(int damage, Photon.Realtime.Player attacker)
    {
        if (photonView.IsMine)
        {
            currentHealth -= damage;
            Debug.Log($"Player {photonView.Owner.NickName} (me) took {damage} damage from {attacker.NickName}. Current health: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Die(attacker);
            }
        }
    }

    void Die(Photon.Realtime.Player killer)
    {
        Debug.Log($"Player {photonView.Owner.NickName} (me) was killed by {killer.NickName}.");
        if (controller != null) controller.enabled = false;
    }

    // [PunRPC]
    // void AnnounceDeath(string killerName)
    // {
    //    Debug.Log($"Player {photonView.Owner.NickName} was announced dead. Killed by {killerName}.");
    // }

    void OnApplicationFocus(bool hasFocus)
    {
        if (photonView.IsMine)
        {
            if (hasFocus)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
