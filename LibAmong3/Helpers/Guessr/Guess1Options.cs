using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers.Guessr
{
    public class Guess1Options
    {
        /// <summary>
        /// Disable detection of Arm64X binaries.
        /// </summary>
        public bool DisableArm64XDetection { get; set; }

        /// <summary>
        /// Use the presence of CHPE metadata pointer to identify Arm64X binaries.
        /// </summary>
        public bool SeeCHPEMetadataPointerForArm64X { get; set; }
    }
}
