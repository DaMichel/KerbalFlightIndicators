using UnityEngine;
using System.Text;

namespace KerbalFlightIndicators
{
#if DEBUG
    public static class DMDebug
    {
        public static void PrintTransformHierarchy(Transform transf, int depth, StringBuilder sb)
        {
            PrintTransform(transf, depth, sb);
            for (int i = 0; i < transf.childCount; ++i)
            {
                PrintTransformHierarchy(transf.GetChild(i), depth + 1, sb);
            }
        }

        public static void PrintTransform(Transform transf, int depth, StringBuilder sb)
        {
            string vfmt = "F3";
            sb.AppendLine(new string(' ', depth) + transf);
            sb.AppendLine("P = " + transf.localPosition.ToString(vfmt));
            sb.AppendLine("R = " + transf.localEulerAngles.ToString(vfmt));
            sb.AppendLine("S = " + transf.localScale.ToString(vfmt));
            sb.AppendLine("MW = \n" + transf.localToWorldMatrix.ToString(vfmt));
            if (transf.parent != null)
            {
                Matrix4x4 ml = transf.parent.localToWorldMatrix.inverse * transf.localToWorldMatrix;
                sb.AppendLine("ML = \n" + ml.ToString(vfmt));
            }
        }

        public static void PrintTransformHierarchyUp(Transform transf, int depth, StringBuilder sb)
        {
            PrintTransform(transf, depth, sb);
            if (transf.parent)
                PrintTransformHierarchyUp(transf.parent, depth + 1, sb);
        }

        // from http://stackoverflow.com/questions/1838963/easy-and-fast-way-to-convert-an-int-to-binary
        public static string ToBin(int value, int len)
        {
            return (len > 1 ? ToBin(value >> 1, len - 1) : null) + "01"[value & 1];
        }

        public static string DebugRepr(Quaternion q)
        {
            Vector3 x = q * Util.x; // apply rotation to vectors
            Vector3 y = q * Util.y;
            Vector3 z = q * Util.z;
            return string.Format("[{0}, {1}, {2}]", x.ToString("F3"), y.ToString("F3"), z.ToString("F3"));
        }
    }
#endif
}
