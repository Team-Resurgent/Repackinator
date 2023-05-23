#version 130
precision mediump float;
precision mediump int;

uniform sampler2D in_fontTexture;

varying vec4 color;
varying vec2 texCoord;

void main()
{
    gl_FragColor = color * texture(in_fontTexture, texCoord);
}