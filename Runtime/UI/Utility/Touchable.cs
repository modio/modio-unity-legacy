/**
 * Code copied and modfied from Unity Answers
 *
 * URL: https://answers.unity.com/questions/801928/46-ui-making-a-button-transparent.html
 * Author: https://answers.unity.com/users/518384/-bcristian.html
 **/

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    public class Touchable : Graphic
    {
        public override bool Raycast(Vector2 sp, Camera eventCamera)
        {
            // return base.Raycast(sp, eventCamera);
            return true;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}
