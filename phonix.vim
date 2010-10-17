" Vim syntax file
" Language:	Phonix
" Maintainer: JS Bangs
" Last Change:	Fri Aug 14 13:56:37 PDT 2009
" Filenames:	*.phonix, *.phx

if exists("b:current_syntax")
    finish
endif

let s:cs_cpo_save = &cpo
set cpo&vim

" core elements
syn keyword phDecl      feature symbol rule syllable import
syn keyword phSyllable  onset nucleus coda

" comments
syn match   phComment   "#.*$"

" strings
syn region  phSqString  start=" '" end="' "
syn region  phDqString  start=+ "+ end=+" +

" matrices
syn region  phMatrix    start="\[" end="]" contains=phValMarker,phNumber

" feature values
syn match   phValMarker "[+\-*=<>]" contained
syn match   phNumber    "\d\+" contained

" parameter lists
syn region  phParamList start="(" end=")" contains=phMatrix

hi def link phDecl      Type
hi def link phSyllable  StorageClass
hi def link phComment   Comment
hi def link phSqString  String
hi def link phDqString  String
hi def link phMatrix    Statement
hi def link phValMarker SpecialChar
hi def link phNumber    Number
hi def link phParamList PreProc
