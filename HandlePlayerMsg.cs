using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//---------------------------------------------
//  handleConnMsg 和 handlePlayerMsg中使用
//  Msg+协议名 来处理对应的协议
//  HandleConnMsg和HandlePlayerMsg使用partial修饰，
//  表明是局部类型，它允许我们将一个类、结构或接口分成几个部分，
//  分别实现再几个不同的.cs文件中。考虑到游戏中有成百上千条协议，
//  难以全部放到同一个文件中，必要时可根据功能模块将逻辑代码分到多个文件。
//---------------------------------------------

namespace ServerStudy
{
    //处理角色协议
    public partial class HandlePlayerMsg
    {

    }
}
