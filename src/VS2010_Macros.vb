'Refer:http://hi.baidu.com/new/dalunzi and http://blog.csdn.net/ajioy/article/details/8483011 ,Thank to Ajioy and Dalunzi
'Author :    Ajioy 、 Dalunzi and Guo
'Mail  :     andyguo1989@gmail.com

Imports System
Imports EnvDTE
Imports EnvDTE80
Imports EnvDTE90
Imports EnvDTE90a
Imports EnvDTE100
Imports System.Diagnostics
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.IO
Imports System.Collections.Specialized
'
Public Module MyMacros
    '在这里设置文件头注释要使用的作者信息
    Dim author As String = "gcy"
    Public Sub FileComment()
        '/****************************************************************
        '*  Test.cpp       author:      date: 13/08/2013
        '*-------------------------------------------------------------
        '*  note: 
        '*  
        ' *****************************************************************/
        'Dim obj = Now()
        Dim Comments As New StringBuilder
        Dim BlankLine As String
        BlankLine = "*  " + vbCrLf 'vbCrLf Cr+ LR,换行
        With Comments
            .AppendFormat("/****************************************************************{0}", vbNewLine)
            .AppendFormat("*  {3}       author: {2}      date: {0}{1}", Date.Today.ToString("dd/MM/yyyy"), vbNewLine, author, DTE.ActiveDocument.Name)
            .AppendFormat("*----------------------------------------------------------------{0}", vbNewLine)
            .AppendFormat("*  note: {0}", vbNewLine)
            .Append(BlankLine)
            .AppendFormat("*****************************************************************/{0}", vbNewLine)
            .Append(vbNewLine)
        End With

        Dim objSel As TextSelection
        objSel = CType(DTE.ActiveDocument.Selection, TextSelection)
        DTE.UndoContext.Open("FileComment")
        objSel.StartOfDocument(False)
        objSel.Insert(Comments.ToString())
        DTE.UndoContext.Close()

    End Sub
    Sub FunctionDeclareComment()
        '函数声明注释，可以自动提取参数列表
        'Dim obj
        'obj = Now()
        Dim InFileType As Integer
        Dim DocSel As EnvDTE.TextSelection
        DocSel = DTE.ActiveDocument.Selection
        '备注下选中行（函数声明行)
        Dim FucNameBak As String
        FucNameBak = DocSel.Text

        Dim FuncName, FuncType, sTmp As String
        Dim SpacePos, LeftPos, RightPos, TabPos, SemiColonPos, LastSpacePos, LastTabPos As Integer
        SpacePos = InStr(1, FucNameBak, " ", 1) '空格出现的位置
        TabPos = InStr(1, FucNameBak, "	", 1)
        LeftPos = InStr(1, FucNameBak, "(", 1)
        RightPos = InStr(1, FucNameBak, ")", 1)
        'SemiColonPos = InStr(1, FucNameBak, ";", 1)
        LastSpacePos = InStrRev(FucNameBak, " ")
        LastTabPos = InStrRev(FucNameBak, "	")

        If SpacePos = 0 And TabPos = 0 Then
            FuncType = ""
            FuncName = Mid(FucNameBak, 1, LeftPos - 1)
        Else
            Dim RelativePos As Integer
            If SpacePos < LeftPos And SpacePos > TabPos Then '取离左括号最近的空格或tab的位置
                RelativePos = SpacePos
            ElseIf TabPos < LeftPos Then
                RelativePos = TabPos
            End If

            FuncType = Mid(FucNameBak, 1, RelativePos)
            'FuncName = Mid(FucNameBak,SpacePos+1,LeftPos-SpacePos-1)
            'FuncName = Mid(FucNameBak, RelativePos + 1, SemiColonPos - RelativePos - 1) '修改输出函数名为带参加的完整名称，即显示到右边括号;
            FuncName = Mid(FucNameBak, RelativePos + 1)
        End If

        Dim ParaArray As Array
        Dim NoneArray As Array
        Dim NoneArrayFlag As Boolean
        NoneArrayFlag = False

        sTmp = Mid(FucNameBak, (LeftPos + 1), (RightPos - LeftPos - 1))
        If sTmp <> "" Then
            ParaArray = Split(sTmp, ",", -1, 1)
        Else
            'ParaArray = NoneArray
            'TODO 如何在生成空的Array
            NoneArrayFlag = True
        End If

        '判断文件类型
        InFileType = FileType(DTE.ActiveDocument)

        If DocSel.IsEmpty() Then
            MsgBox("选中区域为空，请选择函数声明行！", MsgBoxStyle.OkOnly, "重要提示")
            Exit Sub
        End If

        DocSel.Copy()
        DocSel.NewLine()
        '.h和.cpp注释第一行和最后一行不一样
        If InFileType = 0 Then
            '.h
            DocSel.Text = "/**"
        ElseIf InFileType = 1 Then
            '.cpp
            DocSel.Text = "/***********************************************************************"
        End If
        DocSel.NewLine()
        DocSel.Text = "* @brief     " + vbTab
        DocSel.NewLine()

        'If UBound(ParaArray) < 0 Then
        If NoneArrayFlag = True Then
            '没有参数就输出 无
            DocSel.Text = "* @param[in]     无"
            DocSel.NewLine()
            DocSel.Text = "* @param[out]    无"
            DocSel.NewLine()
        Else
            For i = 0 To UBound(ParaArray)
                '不需要参数的类型
                'string strSectionName ,只需要打印出strSectionName即可，类型和参数名之前可能是空格或Tab
                Dim TempParamStr = RTrim(LTrim(ParaArray(i)))
                '将TAB替换成Space
                TempParamStr = Replace(TempParamStr, vbTab, " ")

                SpacePos = InStr(1, TempParamStr, " ", 1) '空格出现的位置
                TabPos = InStr(1, TempParamStr, "	", 1)
                LastSpacePos = InStrRev(TempParamStr, " ") '最后空格出现的位置
                LastTabPos = InStrRev(TempParamStr, "	")

                Dim NewParamStr, ParamType

                'Dim RelativePos As Integer

                'RelativePos = SpacePos

                ParamType = Mid(TempParamStr, 1, SpacePos)
                If LastSpacePos <> 0 Then
                    '字符串尾的空格已经去除，最后一个空格意味着参数名的开始
                    NewParamStr = Mid(TempParamStr, LastSpacePos + 1)
                Else
                    NewParamStr = Mid(TempParamStr, SpacePos + 1)
                End If

                '为了兼容"int * pInt"这种写法，改为判断整个参数字符串中是否包括*或&,而不是判断参数类型中是否包含，因为判断参数类型因为多了个空格可能有误
                If InStr(1, TempParamStr, "&", 1) <> 0 Or InStr(1, TempParamStr, "*", 1) <> 0 Or InStr(1, TempParamStr, "&", 1) <> 0 Or InStr(1, TempParamStr, "*", 1) <> 0 Then
                    DocSel.Text = "* @param[out]   " & NewParamStr
                Else
                    DocSel.Text = "* @param[in]    " & NewParamStr
                End If
                DocSel.NewLine()
            Next
        End If

        FuncType = RTrim(LTrim(FuncType))
        DocSel.Text = "* @return       " + FuncType
        If FuncType = "BOOL" Then
            DocSel.Text = ",TRUE:成功    FALSE:失败"
        End If

        If FuncType = "bool" Then
            'MsgBox(TypeName(FuncType))
            DocSel.Text = ",True:成功,False:失败"
        End If

        DocSel.NewLine()
        If InFileType = 0 Then
            '.h
            DocSel.Text = "*/"
        ElseIf InFileType = 1 Then
            '.cpp
            DocSel.Text = "***********************************************************************/"
        End If
        DocSel.NewLine()
        'DocSel.StartOfLine()
        DocSel.Paste()
    End Sub
    Public Sub DuplicateLine()
        '复制、粘贴当前光标所在行
        'select the current line,copy and paste a same line
        Dim DocSel As EnvDTE.TextSelection
        DocSel = DTE.ActiveDocument.Selection
        DocSel.SelectLine()
        DocSel.Copy()
        DocSel.StartOfLine()
        DocSel.Paste()
        '缩进一格
        DocSel.Indent()
        DocSel.LineUp()

        'DTE.ActiveDocument.Selection().TopPoint.CreateEditPoint().Insert("//123")
    End Sub
    Sub VariableComment()
        '变量声明注释 Variable comment
        'Dim obj
        'obj = Now()
        ActiveDocument.Selection.Text = "/** @brief     */"
    End Sub
    Sub CutLine()
        '剪切一行 Cut current line
        Dim DocSel As EnvDTE.TextSelection
        DocSel = DTE.ActiveDocument.Selection
        DocSel.SelectLine()
        DocSel.Cut()
    End Sub
    Sub AddStartSymbol()
        'DESCRIPTION 开始注释  
        ActiveDocument.Selection.Text = "/*"
    End Sub
    Sub AddEndSymbol()
        'DESCRIPTION 结束注释  
        ActiveDocument.Selection.Text = "*/"
    End Sub
    Public Sub ClassComment()
        'c++ class comment
        Dim DocSel As EnvDTE.TextSelection
        DocSel = DTE.ActiveDocument.Selection
        DocSel.Text = "/**" + vbCrLf
        DocSel.Text = "*@defgroup    " + vbCrLf
        DocSel.Text = "*@{" + vbCrLf
        DocSel.Text = "*/" + vbCrLf + vbCrLf
        DocSel.Text = "/**" + vbCrLf
        DocSel.Text = "*@brief      " + vbCrLf
        DocSel.Text = "*@author     " + author + vbCrLf
        DocSel.Text = "date:        " + Date.Today.ToString("dd/MM/yyyy") + vbCrLf
        DocSel.Text = "*" + vbCrLf
        DocSel.Text = "*example" + vbCrLf
        DocSel.Text = "*@code*" + vbCrLf
        DocSel.Text = "*    " + vbCrLf
        DocSel.Text = "*    " + vbCrLf
        DocSel.Text = "*    " + vbCrLf
        DocSel.Text = "*    " + vbCrLf
        DocSel.Text = "@endcode" + vbCrLf
        DocSel.Text = "*/" + vbCrLf
        '下面就是写类定义代码的地方了
        DocSel.Text = "/** @} */ //OVER" '类定义结果后

    End Sub

    Function FileType(ByVal doc As EnvDTE.Document) As Integer
        ' This routine has many uses if you are trying to determine the type of a source
        '   file.
        ' Return value: -1 Unknown file type
        '               0 .h
        '               1 .cpp
        ' USE: Pass this function the document for which you want to get information.

        Dim pos As Integer
        Dim ext As String
        ext = doc.Name
        FileType = 0

        pos = InStr(ext, ".")
        If pos > 0 Then
            Do While pos <> 1
                ext = Mid(ext, pos, Len(ext) - pos + 1)
                pos = InStr(ext, ".")
            Loop
            ext = LCase(ext)
        End If
        If ext = ".h" Then
            FileType = 0
        ElseIf ext = ".cpp" Then
            FileType = 1
        Else
            FileType = -1
        End If
    End Function

End Module
