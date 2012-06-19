﻿using System;
using System.Collections.Generic;
using Ela.CodeModel;

namespace Ela.Compilation
{
    //This Part is responsible for compilation of type class instances
    internal sealed partial class Builder
    {
        //A name of the type that is currently processed (used when compiling a binding)
        private string currentType;
        
        //A current type prefix (module alias), used when compiling a binding
        private string currentTypePrefix;

        //Main method for instnace compilation
        private void CompileInstance(ElaClassInstance s, LabelMap map, Hints hints)
        {
            //Set the currently processed type
            currentType = s.TypeName;
            currentTypePrefix = s.TypePrefix;

            //Obtain type class data
            var mod = default(CodeFrame);
            var mbr = default(ClassData);
            var modId = -1;
            ObtainTypeClass(s, out mbr, out modId, out mod);
            
            //Type class not found, nothing else to do
            if (mbr == null && (mod == null || !mod.Classes.TryGetValue(s.TypeClassName, out mbr)))
            {
                AddError(ElaCompilerError.UnknownClass, s, s.TypeClassName);
                return;
            }

            //Now we need to obtain a local ID of a module where type is defined
            var typCode = -1;
            ObtainType(s, out typCode);           

            //Add new instance registration information
            frame.Instances.Add(new InstanceData(s.TypeName, s.TypeClassName, typCode, modId, s.Line, s.Column));

            //Fill a list of classMembers, this list is used in this method to validate
            //whether all members of a class have an implementation
            var classMembers = new List<String>(mbr.Members.Length);

            for (var i = 0; i < mbr.Members.Length; i++)
                classMembers.Add(mbr.Members[i].Name);

            var b = s.Where;
            var err = false;

            while (b != null)
            {
                //Patterns in bindings are currently not allowed in instances
                if (b.Pattern != null)
                {
                    AddError(ElaCompilerError.MemberNoPatterns, b.Pattern, b.Pattern.ToString());
                    err = true;
                }

                //Here we mark a binding as 'member binding' so that CompileDeclaration will know that
                //it needs to apply a slightly different logic to it.
                if (!String.IsNullOrEmpty(b.VariableName) && classMembers.Contains(b.VariableName))
                {
                    b.MemberBinding = true;
                    classMembers.Remove(b.VariableName);
                }

                b = b.And;
            }

            //A instance body is empty which is OK if this instance can be automatically generated
            if (s.Where == null && IsAutoGenerated(s))
                return;

            //Not all of the members are implemented, which is an error
            if (classMembers.Count > 0)
                AddError(ElaCompilerError.MemberNotAll, s, s.TypeClassName, s.TypeClassName + " " + s.TypeName);

            //If there are no errors we can compile members
            if (!err)
            {
                StartScope(false, s.Where.Line, s.Where.Column);
                CompileDeclaration(s.Where, map, hints);
                EndScope();
            }
        }

        //This method returns type class data including: type class metadata (ClassData), module local ID
        //(modId) and compiled module where class is defined.
        private void ObtainTypeClass(ElaClassInstance s, out ClassData mbr, out int modId, out CodeFrame mod)
        {
            mbr = null;
            modId = -1;
            mod = null;

            //If a type class prefix is not set we need to obtain a type ID using a special '$$$*' variable
            //that is initialized during type class compilation
            if (s.TypeClassPrefix == null)
            {
                //We first check if a class definition is non-local
                if (!frame.Classes.TryGetValue(s.TypeClassName, out mbr))
                {
                    var sv = GetVariable("$$$" + s.TypeClassName, CurrentScope, GetFlags.NoError, s.Line, s.Column);

                    if (sv.IsEmpty())
                    {
                        AddError(ElaCompilerError.UnknownClass, s, s.TypeClassName);
                        return;
                    }

                    //The trick is - here sv can be only an external name (we do check prior to this
                    //if a class is not local). If it is an external name that first byte contains a
                    //local index of a referenced module - that is exactly what we need here to obtain
                    //a compiled module frame (from refs array).
                    modId = sv.Address & Byte.MaxValue;
                    mod = refs[modId];
                }
            }
            else
            {
                //Type class prefix is set. The prefix itself should be local name (a module alias)
                var sv = GetVariable(s.TypeClassPrefix, s.Line, s.Column);

                if (sv.IsEmpty())
                    return;

                //A name was found but this is not a module alias
                if ((sv.Flags & ElaVariableFlags.Module) != ElaVariableFlags.Module)
                {
                    AddError(ElaCompilerError.InvalidQualident, s, s.TypeClassPrefix);
                    return;
                }

                //In this case we can look for a reference based on its alias (alias should be unique within
                //the current module).
                modId = frame.References[s.TypeClassPrefix].LogicalHandle;
                mod = refs[modId];
            }
        }

        //Here we obtain type information - 
        private void ObtainType(ElaClassInstance s, out int typCode)
        {
            typCode = -1;

            if (s.TypePrefix == null)
            {
                //First we check that type is not defined locally
                if (!frame.Types.ContainsKey(s.TypeName))
                {
                    var sv = GetVariable("$$" + s.TypeName, CurrentScope, GetFlags.NoError, s.Line, s.Column);

                    if (sv.IsEmpty())
                    {
                        AddError(ElaCompilerError.UndefinedType, s, s.TypeName);
                        return;
                    }

                    //The trick is - here sv can be only an external name (we do check prior to this
                    //if a type is not local). If it is an external name that first byte contains a
                    //local index of a referenced module - and typCode is effectly a local ID of a module
                    //where a type is declared
                    typCode = sv.Address & Byte.MaxValue;
                }
            }
            else
            {
                //TypePrefix is a local name that should correspond to a module alias
                var sv = GetVariable(s.TypePrefix, s.Line, s.Column);

                if (sv.IsEmpty())
                    return;

                //A name exists but this is not a module alias
                if ((sv.Flags & ElaVariableFlags.Module) != ElaVariableFlags.Module)
                {
                    AddError(ElaCompilerError.InvalidQualident, s, s.TypePrefix);
                    return;
                }

                //Obtain a local ID of a module based on TypePrefix (which is module alias
                //that should be unique within this module).
                typCode = frame.References[s.TypePrefix].LogicalHandle;
            }
        }

        //An instance might be missing a body. If this instance corresponds to
        //an auto-generated instance we are OK with this, otherwise we will generate
        //an error (on the caller side).
        private bool IsAutoGenerated(ElaClassInstance b)
        {
            if (b.TypePrefix != null || b.TypeClassPrefix != null ||
                TypeCodeFormat.GetTypeCode(b.TypeName) == ElaTypeCode.None)
                return false;

            switch (b.TypeClassName)
            {
                case "Typeable":
                case "Eq":
                case "Ord":
                case "Num":
                case "Bit":
                case "Enum":
                case "Seq":
                case "Ix":
                case "Cons":
                case "Cat":
                case "Show":
                    return true;
                default:
                    return false;
            }
        }
    }
}
