using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LibAmong3.Helpers.PE32.DvrtModel1
{
    public class Arm64X
    {
        [XmlElement] public Group[]? Group { get; set; }
    }
}
