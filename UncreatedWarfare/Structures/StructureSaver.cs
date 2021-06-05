using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Structures
{
    public class StructureSaver : JSONSaver<Structure>
    {
        public List<Structure> ActiveStructures;
        public StructureSaver() : base(Data.StructureStorage + "structures.json") { }
        protected override string LoadDefaults() => "[]";
    }

    public class Structure
    {
        public ushort id;
    }
}
