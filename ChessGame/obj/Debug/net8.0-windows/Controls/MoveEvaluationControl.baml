   M S B A M L   `   `   `  ����  ChessGame.WPF2    ,ChessGame.WPF.Controls.MoveEvaluationControl   5      W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      .��5	      �� 
�������  �� CornerRadius$  8��5
      $��5q�5       J� Effect 5   
   3  -System.Windows.Media.Effects.DropShadowEffect  5         Color$ Black��6      
BlurRadius$ 10Q�6-      ShadowDepth$ 2Q�5         Opacity$	 0.2Q�6/   ._�5   
   �� $��10q�6   .+�5      �� $��최근 수 평가��6   $��14(�5      $
��Bold"�5      
���   �$��0,0,0,5q�5      �� 5      $
��Auto=�6   $	��300��5      .��5      �� -   5         ��Name$ 	MovesList��5      ��5      �� 5      .R�5      �� $	��0,2q�6%    �� Padding$ 5q�5   %   $  3��5   %   ��5    %   �� *BackgroundColor+._�5!   "   �  � ColumnDefinitions 5"   &   �� 5#   *   $��45 �6;   �� 5$   *   $��* �6;   �� 5%   *   $��35 �6;   .+�5(   &   �� $��0��60   $
��Bold"�5*   0   
���   �$��12(�5,   0   ��5)   0   �� *
MoveNumber+5,   0   �� 5-   &   $��1��60   $	��5,0q�5/   0   
���   �$��12(�51   0   ��5.   0   �� *
MoveText+51   0   �� 52   &   $��2��60   $
��Bold"�54   0   $��Right=�56   0   $��14(�57   0   ��53   0   �� *	Quality+��55   0   �� *QualityColor+57   0   58   #   59      5:      5<      5=      5>      5?      5@      