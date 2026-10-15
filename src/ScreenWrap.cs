using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenWrap : MonoBehaviour
{
    public delegate void BoundsEnteredHandler(Vector2 boundsDir);

    public event BoundsEnteredHandler BoundsEntered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Bounds"))
        {
            Vector3 newPos = Vector3.zero;
            Vector2 targetOffset = Vector2.zero;

            if (other.gameObject.name.Contains("Bottom"))
            {
                GameObject newObject = GameObject.Find("TopBounds");
                newPos = new Vector3(transform.position.x, newObject.transform.position.y, transform.position.z) + new Vector3(0, -1, 0);
                targetOffset = new Vector2(0, -1);
            }
            else if (other.gameObject.name.Contains("Top"))
            {
                GameObject newObject = GameObject.Find("BottomBounds");
                newPos = new Vector3(transform.position.x, newObject.transform.position.y, transform.position.z) + new Vector3(0, 1, 0);
                targetOffset = new Vector2(0, 1);
            }
            else if (other.gameObject.name.Contains("Left"))
            {
                GameObject newObject = GameObject.Find("RightBounds");
                newPos = new Vector3(newObject.transform.position.x, transform.position.y, transform.position.z) + new Vector3(-1, 0, 0);
                targetOffset = new Vector2(-1, 0);
            }
            else if (other.gameObject.name.Contains("Right"))
            {
                GameObject newObject = GameObject.Find("LeftBounds");
                newPos = new Vector3(newObject.transform.position.x, transform.position.y, transform.position.z) + new Vector3(1, 0, 0);
                targetOffset = new Vector2(1, 0);
            }

            transform.position = newPos;

            if (BoundsEntered != null)
                BoundsEntered(targetOffset);
        }
    }
}
