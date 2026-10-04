using UnityEngine;

namespace SQZL.Entity {
   /// <summary>
   /// Components that should respond to the tile collisions system should implement this interface AND use the tag
   /// HandlesTilebodyCollisions
   /// </summary>
   public interface ITilebodyCollisionHandler
   {
      /// <summary>
      /// Invoked when a tilebody collides with this game object for every component that implements this interface.
      /// To avoid unnecessary checks/improve performance this interface will only be checked for if your gameobject
      /// has the tag HandlesTilebodyCollisions.
      /// </summary>
      /// <param name="body">The Tilebody that collided with this gameobject</param>
      public abstract void OnCollideWithTilebody(Tilebody2D body);
   }
}
