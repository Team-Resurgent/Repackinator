#version 130
precision mediump float;
precision mediump int;

uniform mat4 projection_matrix;

attribute vec2 in_position;
attribute vec2 in_texCoord;
attribute vec4 in_color;

varying vec4 color;
varying vec2 texCoord;

void main()
{
    gl_Position = projection_matrix * vec4(in_position, 0, 1);
    color = in_color;
    texCoord = in_texCoord;
}