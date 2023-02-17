using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClimbController : MonoBehaviour
{
    ClimbPoint currentPoint;

    PlayerController playerController;
    EnvironmentScanner envScanner;
    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        envScanner = GetComponent<EnvironmentScanner>();
    }

    private void Update()
    {
        if (!playerController.IsHanging)
        {
            if (Input.GetButton("Jump") && !playerController.InAction)
            {
                if (envScanner.ClimbLedgeCheck(transform.forward, out RaycastHit ledgeHit))
                {
                    currentPoint = ledgeHit.transform.GetComponent<ClimbPoint>();

                    playerController.SetControl(false);
                    StartCoroutine(JumpToLedge("IdleToHang", ledgeHit.transform, 0.41f, 0.54f));
                }
            }
        }
        else
        {
            if(Input.GetButton("Drop") && !playerController.InAction)
            {
                StartCoroutine(JumpFromHang());
                return;
            }
            // Ledge to Ledge Jump

            float h = Mathf.Round(Input.GetAxisRaw("Horizontal"));
            float v = Mathf.Round(Input.GetAxisRaw("Vertical"));
            var inputDir = new Vector2(h, v);

            if (playerController.InAction || inputDir == Vector2.zero) return;

            if(currentPoint.MountPoint && inputDir.y == 1)
            {
                StartCoroutine(MountFromHang());
                
                return;
            }

            var neighbour = currentPoint.GetNeighbour(inputDir);
            if (neighbour == null) return;

            if (neighbour.connectionType == ConnectionType.Jump && Input.GetButton("Jump"))
            {
                currentPoint = neighbour.point;

                if (neighbour.direction.y == 1)
                    StartCoroutine(JumpToLedge("HangHopUp", currentPoint.transform, 0.35f, 0.65f, handOffset: new Vector3(0.25f, 0f, 0.15f)));
                else if (neighbour.direction.y == -1)
                    StartCoroutine(JumpToLedge("HangHopDown", currentPoint.transform, 0.31f, 0.65f, handOffset: new Vector3(0.25f, 0f, 0.13f)));
                else if (neighbour.direction.x == 1)
                    StartCoroutine(JumpToLedge("HangHopRight", currentPoint.transform, 0.20f, 0.50f));
                else if (neighbour.direction.x == -1)
                    StartCoroutine(JumpToLedge("HangHopLeft", currentPoint.transform, 0.20f, 0.50f));
            }
            else if (neighbour.connectionType == ConnectionType.Move)
            {
                currentPoint = neighbour.point;

                if (neighbour.direction.x == 1)
                    StartCoroutine(JumpToLedge("ShimmyRight", currentPoint.transform, 0f, 0.38f, handOffset: new Vector3(0.25f, 0f, 0.1f)));
                else if (neighbour.direction.x == -1)
                    StartCoroutine(JumpToLedge("ShimmyLeft", currentPoint.transform, 0f, 0.38f, AvatarTarget.LeftHand, handOffset: new Vector3(0.25f, 0f, 0.1f)));
            }
        }
    }

    IEnumerator JumpToLedge(string anim, Transform ledge, float matchStartTime, float matchTargetTime,
        AvatarTarget hand=AvatarTarget.RightHand,
        Vector3? handOffset=null)
    {
        var matchParams = new MatchTargetParams()
        {
            pos = GetHandPos(ledge, hand, handOffset),
            bodyPart = hand,
            startTime = matchStartTime,
            targetTime = matchTargetTime,
            posWeight = Vector3.one
        };

        var targetRot = Quaternion.LookRotation(-ledge.forward);

        yield return playerController.DoAction(anim, matchParams, targetRot, true);

        playerController.IsHanging = true;
    }

    Vector3 GetHandPos(Transform ledge, AvatarTarget hand, Vector3? handOffset)
    {
        var offVal = (handOffset != null) ? handOffset.Value : new Vector3(0.25f, 0f, 0.1f);

        var hDir = (hand == AvatarTarget.RightHand) ? ledge.right : -ledge.right;
        return ledge.position + ledge.forward * offVal.z + Vector3.up * offVal.y - hDir * offVal.x;
    }

    private IEnumerator JumpFromHang()
    {
        playerController.IsHanging = false;
        yield return playerController.DoAction("JumpFromHang");
        playerController.ResetRotation();
        playerController.SetControl(true);
    }

    private IEnumerator MountFromHang()
    {
        playerController.IsHanging = false;
        yield return playerController.DoAction("MountFromHang");

        playerController.EnableCharController(true);
        yield return new WaitForSeconds(0.5f);

        playerController.ResetRotation();
        playerController.SetControl(true);
    }
}
