using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {
	private int x, y;
	private Vector3 position;
	private UpdateMazePosition ump;

	void Start ()
	{
		ump = (GameObject.Find("GameController") as GameObject).GetComponent("UpdateMazePosition") as UpdateMazePosition;
	}
	// Update is called once per frame
	private void Update () 
	{
		position = this.transform.position;
		x = (int) this.transform.position.x;
		y = (int) this.transform.position.y;
		if(!UpdateMazePosition.gameOver)
		{
			if((Input.GetKeyDown("w") || Input.GetKeyDown(KeyCode.UpArrow)) && y < UpdateMazePosition.maxAxis)
				if(UpdateMazePosition.checkNorth(position))
					this.transform.position = this.transform.position + Vector3.up;
			if((Input.GetKeyDown("a") || Input.GetKeyDown(KeyCode.LeftArrow)) && x > 0)
				if(UpdateMazePosition.checkWest(position))
					this.transform.position = this.transform.position + Vector3.left;
			if((Input.GetKeyDown("s") || Input.GetKeyDown(KeyCode.DownArrow)) && y > 0)
				if(UpdateMazePosition.checkSouth(position))
					this.transform.position = this.transform.position + Vector3.down;
			if((Input.GetKeyDown("d") || Input.GetKeyDown(KeyCode.RightArrow)) && x < UpdateMazePosition.maxAxis)
				if(UpdateMazePosition.checkEast(position))
					this.transform.position = this.transform.position + Vector3.right;
		}

		if(Input.GetKeyDown("r"))
		{
			resetPlayer();
		}
		if(Input.GetKeyDown("t"))
		{
			UpdateMazePosition.score = 0;
			ump.tick();
		}
	}

	private void resetPlayer()
	{
		this.transform.position = Vector3.zero;
		UpdateMazePosition.gameOver = false;
	}
}
