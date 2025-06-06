using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ByteForge.Runtime
{
    /// <summary>
    /// A specialized Image component that creates a cutout mask effect for UI elements.
    /// </summary>
    /// <remarks>
    /// This component functions as the inverse of Unity's standard Mask component.
    /// While the standard Mask shows content only inside the mask area, this CutoutMask
    /// shows content everywhere except inside the mask area.
    /// 
    /// This works by modifying the stencil comparison function to NotEqual,
    /// effectively inverting the behaviour of the standard mask.
    /// 
    /// Usage:
    /// 1. Add this component to a UI element
    /// 2. Configure the shape as you would with a regular Image
    /// 3. Child elements will be visible except where they overlap with this mask
    /// 
    /// This is useful for creating effects like punching holes in UI panels or
    /// creating windows through which background elements are not visible.
    /// </remarks>
    public class CutoutMask : Image
    {
        /// <summary>
        /// Overrides the material used for rendering to modify its stencil properties.
        /// </summary>
        /// <remarks>
        /// This property gets the base material from the Image component and
        /// modifies its stencil comparison function to NotEqual, which inverts
        /// the standard masking behaviour.
        /// </remarks>
        public override Material materialForRendering
        {
            get
            {
                Material material = base.materialForRendering;
                material.SetInt("_StencilComp", (int)CompareFunction.NotEqual);

                return material;
            }
        }
    }
}