using System;
using System.Collections.Generic;

namespace ParadoxNotion.Serialization.FullSerializer {
    partial class fsConverterRegistrar {
        public static Internal.DirectConverters.Vector4_DirectConverter Register_Vector4_DirectConverter;
    }
}

namespace ParadoxNotion.Serialization.FullSerializer.Internal.DirectConverters {
    public class Vector4_DirectConverter : fsDirectConverter<UnityEngine.Vector4> {
        protected override fsResult DoSerialize(UnityEngine.Vector4 model, Dictionary<string, fsData> serialized) {
            var result = fsResult.Success;

            result += SerializeMember(serialized, "x", model.x);
            result += SerializeMember(serialized, "y", model.y);
            result += SerializeMember(serialized, "z", model.z);
            result += SerializeMember(serialized, "w", model.w);

            return result;
        }

        protected override fsResult DoDeserialize(Dictionary<string, fsData> data, ref UnityEngine.Vector4 model) {
            var result = fsResult.Success;

            var t0 = model.x;
            result += DeserializeMember(data, "x", out t0);
            model.x = t0;

            var t1 = model.y;
            result += DeserializeMember(data, "y", out t1);
            model.y = t1;

            var t2 = model.z;
            result += DeserializeMember(data, "z", out t2);
            model.z = t2;

            var t3 = model.w;
            result += DeserializeMember(data, "w", out t3);
            model.w = t3;

            return result;
        }

        public override object CreateInstance(fsData data, Type storageType) {
            return new UnityEngine.Vector4();
        }
    }
}
